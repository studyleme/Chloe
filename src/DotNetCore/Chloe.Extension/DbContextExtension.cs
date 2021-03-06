﻿using Chloe.Descriptors;
using Chloe.Exceptions;
using Chloe.Extension;
using Chloe.InternalExtensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Chloe
{
    public static partial class DbContextExtension
    {
        public static IQuery<T> Query<T>(this IDbContext dbContext, Expression<Func<T, bool>> predicate)
        {
            return dbContext.Query<T>().Where(predicate);
        }
        /// <summary>
        /// context.SqlQuery&lt;User&gt;("select * from Users where Id>@Id", new { Id = 1 }).ToList();
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="sql"></param>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public static IEnumerable<T> SqlQuery<T>(this IDbContext dbContext, string sql, object parameter)
        {
            /*
             * Usage:
             * context.SqlQuery<User>("select * from Users where Id>@Id", new { Id = 1 }).ToList();
            */

            return dbContext.SqlQuery<T>(sql, BuildParams(dbContext, parameter));
        }
        public static IEnumerable<T> SqlQuery<T>(this IDbContext dbContext, string sql, CommandType cmdType, object parameter)
        {
            /*
             * Usage:
             * context.SqlQuery<User>("select * from Users where Id>@Id", CommandType.Text, new { Id = 1 }).ToList();
            */

            return dbContext.SqlQuery<T>(sql, cmdType, BuildParams(dbContext, parameter));
        }

        public static void BeginTransaction(this IDbContext dbContext)
        {
            dbContext.Session.BeginTransaction();
        }
        public static void BeginTransaction(this IDbContext dbContext, IsolationLevel il)
        {
            dbContext.Session.BeginTransaction(il);
        }
        public static void CommitTransaction(this IDbContext dbContext)
        {
            dbContext.Session.CommitTransaction();
        }
        public static void RollbackTransaction(this IDbContext dbContext)
        {
            dbContext.Session.RollbackTransaction();
        }
        public static void DoWithTransaction(this IDbContext dbContext, Action action)
        {
            /*
            * context.DoWithTransaction(() =>
            * {
            *     context.Insert()...
            *     context.Update()...
            *     context.Delete()...
            * });
            */

            dbContext.Session.BeginTransaction();
            ExecuteAction(dbContext, action);
        }
        public static void DoWithTransaction(this IDbContext dbContext, Action action, IsolationLevel il)
        {
            dbContext.Session.BeginTransaction(il);
            ExecuteAction(dbContext, action);
        }
        public static T DoWithTransaction<T>(this IDbContext dbContext, Func<T> action)
        {
            dbContext.Session.BeginTransaction();
            return ExecuteAction(dbContext, action);
        }
        public static T DoWithTransaction<T>(this IDbContext dbContext, Func<T> action, IsolationLevel il)
        {
            dbContext.Session.BeginTransaction(il);
            return ExecuteAction(dbContext, action);
        }


        static void ExecuteAction(IDbContext dbContext, Action action)
        {
            try
            {
                action();
                dbContext.Session.CommitTransaction();
            }
            catch
            {
                if (dbContext.Session.IsInTransaction)
                    dbContext.Session.RollbackTransaction();
                throw;
            }
        }
        static T ExecuteAction<T>(IDbContext dbContext, Func<T> action)
        {
            try
            {
                T ret = action();
                dbContext.Session.CommitTransaction();
                return ret;
            }
            catch
            {
                if (dbContext.Session.IsInTransaction)
                    dbContext.Session.RollbackTransaction();
                throw;
            }
        }

        public static DbActionBag CreateActionBag(this IDbContext dbContext)
        {
            DbActionBag bag = new DbActionBag(dbContext);
            return bag;
        }

        static string GetParameterPrefix(IDbContext dbContext)
        {
            Type dbContextType = dbContext.GetType();
            while (true)
            {
                if (dbContextType == null)
                    break;

                string dbContextTypeName = dbContextType.Name;
                switch (dbContextTypeName)
                {
                    case "MsSqlContext":
                    case "SQLiteContext":
                        return "@";
                    case "MySqlContext":
                        return "?";
                    case "OracleContext":
                        return ":";
                    default:
                        dbContextType = dbContextType.BaseType;
                        break;
                }
            }

            throw new NotSupportedException(dbContext.GetType().FullName);
        }
        static DbParam[] BuildParams(IDbContext dbContext, object parameter)
        {
            List<DbParam> parameters = new List<DbParam>();

            if (parameter != null)
            {
                string parameterPrefix = GetParameterPrefix(dbContext);
                Type parameterType = parameter.GetType();
                var props = parameterType.GetProperties();
                foreach (var prop in props)
                {
                    if (prop.GetGetMethod() == null)
                    {
                        continue;
                    }

                    object value = ReflectionExtension.GetMemberValue(prop, parameter);

                    string paramName = parameterPrefix + prop.Name;

                    DbParam p = new DbParam(paramName, value, prop.PropertyType);
                    parameters.Add(p);
                }
            }

            return parameters.ToArray();
        }
    }
}
