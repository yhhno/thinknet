﻿using System.Collections.Generic;
using System.Linq;
using ThinkNet.Configurations;
using ThinkNet.Database;


namespace UserRegistration.ReadModel
{
    [UnderlyingComponent(typeof(UserDao))]
    public interface IUserDao
    {
        UserModel Find(string loginid);

        IEnumerable<UserModel> GetAll();
    }

    public class UserDao : IUserDao
    {
        private readonly IDataContextFactory contextFactory;
        public UserDao(IDataContextFactory contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        #region IUserDao 成员

        public UserModel Find(string loginid)
        {
            return this.GetAll().FirstOrDefault(p => p.LoginId == loginid);
        }

        public IEnumerable<UserModel> GetAll()
        {
            //return Enumerable.Empty<UserModel>();
            return contextFactory.CreateDataContext().CreateQuery<UserModel>();
        }

        #endregion
    }
}
