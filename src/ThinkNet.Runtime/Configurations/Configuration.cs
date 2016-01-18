﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Practices.ServiceLocation;
using ThinkLib.Common;
using ThinkLib.Logging;
using ThinkLib.Utilities;
using ThinkNet.Infrastructure;


namespace ThinkNet.Configurations
{
    /// <summary>
    /// 引导程序
    /// </summary>
    public sealed class Configuration
    {
        public class TypeRegistration
        {
            public TypeRegistration(Type type, object instance, string name)
                : this(type, name)
            {
                this.Instance = instance;
            }

            public TypeRegistration(Type type, string name)
                : this(type, name, Configurations.Lifecycle.Singleton)
            { }

            public TypeRegistration(Type type, string name, Lifecycle lifecycle)
            {
                this.RegisterType = type;
                this.Name = name ?? string.Empty;
                this.Lifecycle = lifecycle;
            }

            public TypeRegistration(Type from, Type to, string name, Lifecycle lifecycle)
                : this(from, name, lifecycle)
            {
                this.ImplementationType = to;
            }

            public string Name { get; set; }

            public Type RegisterType { get; private set; }

            public object Instance { get; private set; }

            public Type ImplementationType { get; private set; }

            public Lifecycle Lifecycle { get; private set; }

            public override bool Equals(object obj)
            {
                var other = obj as TypeRegistration;

                if (other == null)
                    return false;

                if (this.RegisterType != other.RegisterType)
                    return false;

                if (!String.Equals(this.Name, other.Name))
                    return false;

                return true;
            }

            public override int GetHashCode()
            {
                return String.Concat(this.RegisterType.FullName, "|", this.Name).GetHashCode();
            }
        }


        /// <summary>
        /// 当前配置
        /// </summary>
        public static readonly Configuration Current = new Configuration();


        private readonly List<Assembly> _assemblies;
        private readonly List<IInitializer> _initializers;
        private readonly List<KeyValuePair<Type, string>> _initializeTypes;
        private readonly HashSet<TypeRegistration> _registeredComponents;
        private Configuration()
        {
            this._assemblies = new List<Assembly>();
            this._initializers = new List<IInitializer>();
            this._initializeTypes = new List<KeyValuePair<Type, string>>();
            this._registeredComponents = new HashSet<TypeRegistration>();
        }
        

        /// <summary>
        /// 加载程序集
        /// </summary>
        public Configuration LoadAssemblies(Assembly[] assemblies)
        {
            _assemblies.Clear();
            _assemblies.AddRange(assemblies);

            return this;
        }

        /// <summary>
        /// 扫描bin目录的程序集
        /// </summary>
        public Configuration LoadAssemblies()
        {
            string applicationAssemblyDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Bin");
            if (!FileUtils.DirectoryExists(applicationAssemblyDirectory)) {
                applicationAssemblyDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }


            var assemblies = Directory.GetFiles(applicationAssemblyDirectory)
                .Where(file => {
                    var ext = Path.GetExtension(file).ToLower();
                    return ext.EndsWith(".dll") || ext.EndsWith(".exe");
                })
                .Select(Assembly.LoadFrom)
                //.Where(assembly => assembly.IsDefined<ParticipateInRuntimeAttribute>(false))
                //.OrderBy(assembly => assembly.GetAttribute<ParticipateInRuntimeAttribute>(false).Order)
                .ToArray();

            return this.LoadAssemblies(assemblies);
        }
        
        private object Resolve(KeyValuePair<Type, string> serviceType)
        {
            return ServiceLocator.Current.GetInstance(serviceType.Key, serviceType.Value ?? string.Empty);
        }            


        private bool _running = false;
        /// <summary>
        /// 配置完成。
        /// </summary>
        public void Done(Action<TypeRegistration> typeRegistry)
        {
            if (_running)
                return;


            if (_assemblies.Count == 0) {
                this.LoadAssemblies();

                LogManager.GetLogger("ThinkNet").DebugFormat("load assemblies[{0}] completed.",
                    string.Join(",", _assemblies.Select(item => item.FullName)));
            }


            var allTypes = _assemblies.SelectMany(assembly => assembly.GetTypes()).ToArray();

            allTypes.Where(IsRegisteredComponent).ForEach(RegisterComponent);
            allTypes.Where(IsRequiredComponent).ForEach(RegisterRequiredComponent);

            _registeredComponents.ForEach(typeRegistry);
            _initializeTypes.Select(Resolve).OfType<IInitializer>().Concat(_initializers)
                .ForEach(initializer => initializer.Initialize(allTypes));
            
            _assemblies.Clear();
            _initializers.Clear();
            _initializeTypes.Clear();
            _registeredComponents.Clear();

            ServiceLocator.Current.GetAllInstances(typeof(IProcessor)).OfType<IProcessor>().ForEach(p => p.Start());
            

            _running = true;

            LogManager.GetLogger("ThinkNet").Debug("system is running.");
        }

        
        /// <summary>
        /// 注册实例
        /// </summary>
        public Configuration RegisterInstance(Type type, object instance, string name = null)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register instance, please execute before 'done' method.");
            }

            Ensure.NotNull(type, "type");
            Ensure.NotNull(instance, "instance");

            _registeredComponents.Add(new TypeRegistration(type, instance, name));

            if (IsInitializer(instance)) {
                _initializers.Add((IInitializer)instance);
            }

            return this;
        }

        /// <summary>
        /// 注册类型
        /// </summary>
        public Configuration RegisterType(Type type, Lifecycle lifecycle = Lifecycle.Singleton, string name = null)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register type, please execute before 'done' method.");
            }

            Ensure.NotNull(type, "type");

            var result = _registeredComponents.Add(new TypeRegistration(type, name, lifecycle));
            if (lifecycle == Lifecycle.Singleton && IsInitializerType(type)) {
                _initializeTypes.Add(new KeyValuePair<Type, string>(type, name));
            }

            return this;
        }
        
        /// <summary>
        /// 注册类型
        /// </summary>
        public Configuration RegisterType(Type from, Type to, Lifecycle lifecycle = Lifecycle.Singleton, string name = null)
        {
            if (_running) {
                throw new ApplicationException("system is running, can not register type, please execute before 'done' method.");
            }

            Ensure.NotNull(from, "from");
            Ensure.NotNull(to, "to");

            _registeredComponents.Add(new TypeRegistration(from, to, name, lifecycle));
            if (lifecycle == Lifecycle.Singleton && IsInitializerType(to)) {
                _initializeTypes.Add(new KeyValuePair<Type, string>(from, name));
            }

            return this;
        }


        private static bool IsInitializerType(Type type)
        {
            return type.IsClass && !type.IsAbstract && typeof(IInitializer).IsAssignableFrom(type);
        }

        private static bool IsInitializer(object instance)
        {
            return instance is IInitializer;
        }

        private bool IsRegisteredComponent(Type type)
        {
            return type.IsClass && !type.IsAbstract && type.IsDefined<RegisterComponentAttribute>(false);
        }

        private bool IsRequiredComponent(Type type)
        {
            return type.IsDefined<RequiredComponentAttribute>(false);
        }

        private void RegisterComponent(Type type)
        {
            var components = type.GetAttributes<RegisterComponentAttribute>(false);
            foreach (var component in components) {
                var name = component.GetFinalRegisterName(type);
                var lifecycle = (Lifecycle)LifeCycleAttribute.GetLifecycle(type);
                var registerType = component.RegisterType;

                if (registerType == null) {
                    this.RegisterType(type, lifecycle, name);
                }
                else {
                    this.RegisterType(registerType, type, lifecycle, name);
                }
            }
        }

        private void RegisterRequiredComponent(Type type)
        {
            var components = type.GetAttributes<RequiredComponentAttribute>(false);
            foreach (var component in components) {
                var name = component.GetFinalRegisterName();
                var lifecycle = (Lifecycle)LifeCycleAttribute.GetLifecycle(type);
                var serviceType = component.ServiceType;

                if (lifecycle == Lifecycle.Singleton) {
                    var member = serviceType.GetMember("Instance", MemberTypes.Field | MemberTypes.Property,
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.IgnoreCase).FirstOrDefault();

                    if (member != null) {
                        this.RegisterInstance(type, member.GetMemberValue(null), name);
                        continue;
                    }

                    if (component.CreateInstance) {
                        var instance = component.ConstructorParameters == null || component.ConstructorParameters.Length == 0 ?
                            Activator.CreateInstance(serviceType) : Activator.CreateInstance(serviceType, component.ConstructorParameters);
                        this.RegisterInstance(type, instance, name);
                        continue;
                    }
                }

                if (serviceType == null) {
                    this.RegisterType(type, lifecycle, name);
                }
                else {
                    this.RegisterType(type, serviceType, lifecycle, name);
                }
            }
        }
    }
}
