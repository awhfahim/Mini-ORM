using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Assignment3
{
    public class MyORM<G, T>
        where T : class
    {
        public void Insert(T entity)
            => InsertRecord(entity);

        public void Update(T entity)
            => UpdateRecord(entity);

        public void Delete(T entity)
        {
            var id_val = entity.GetType().GetProperty("Id")!.GetValue(entity);
            DeleteRecord(entity.GetType(), ("Id", id_val!));
        }

        public ICollection<T> GetAll()
        {
            IList<T> entities = new List<T>();
            AdoNetUtililty adoNetUtil = new();
            var data = adoNetUtil.GetAll(typeof(T).Name);
            foreach (var entity in data)
            {
                var constructor = typeof(T).GetConstructors().FirstOrDefault();
                var instance = constructor?.Invoke(new object[] { });

                if (instance is not null)
                {
                    foreach (var property in entity)
                    {
                        var property_info = instance.GetType().GetProperty(property.columnName);
                        if(property_info is not null)
                            property_info.SetValue(instance, property.value);
                    }
                }

                foreach (var property in typeof(T).GetProperties())
                {
                    var propertyType = property.PropertyType;
                    if (!IsPrimitive(propertyType))
                    {
                        var result = GetAllRecords(propertyType, typeof(T).Name + "Id",
                         instance!.GetType().GetProperty("Id")!.GetValue(instance)!);

                        property.SetValue(instance, result);
                    }
                }

                entities.Add((T)instance!);
            }

            return entities;
        }

        public void Delete(G id)
            => DeleteRecord(typeof(T), ("Id", id!));

        public T? GetById(G id)
        {
            AdoNetUtililty adoNetUtil = new();
            var data = adoNetUtil.GetById(typeof(T).Name, "Id", id!);

            var constructor = typeof(T).GetConstructors().FirstOrDefault();
            var instance = constructor?.Invoke(Array.Empty<object>());

            foreach (var entity in data)
            {             
                if (instance is not null)
                {
                    foreach (var property in entity)
                    {
                        var property_info = instance.GetType().GetProperty(property.columnName);
                        if (property_info is not null)
                            property_info.SetValue(instance, property.value);
                    }
                }

                foreach (var property in typeof(T).GetProperties())
                {
                    var propertyType = property.PropertyType;
                    if (!IsPrimitive(propertyType))
                    {
                        var result = GetAllRecords(propertyType, typeof(T).Name + "Id",
                         instance!.GetType().GetProperty("Id")!.GetValue(instance)!);

                        property.SetValue(instance, result);
                    }
                }
            }
            return data.Count is 0 ? null : (T)instance!;
        }

        private object? GetAllRecords(Type type, string keyName, object id)
        {
            AdoNetUtililty adoNetUtil = new();         

            if (type.GetInterfaces().Contains(typeof(IList)))
            {
                var propertyTypeName = type.GetGenericArguments().First();
                var data = adoNetUtil.GetById(propertyTypeName.Name, keyName, id);

                Type listType = typeof(List<>).MakeGenericType(propertyTypeName);
                var myList = listType.GetConstructors().FirstOrDefault()!.Invoke(Array.Empty<object>());

                foreach(var value in data)
                {
                    var instance = propertyTypeName.GetConstructors().FirstOrDefault()!.Invoke(Array.Empty<object>());
                    foreach(var item in value)
                    {
                        var propertyInstance = instance.GetType().GetProperty(item.columnName);
                        if (propertyInstance is not null)
                            propertyInstance.SetValue(instance, item.value);
                    }

                    foreach (var property in propertyTypeName.GetProperties())
                    {
                        var propertyType = property.PropertyType;
                        if (!IsPrimitive(propertyType))
                        {
                            var val = instance.GetType().GetProperty("Id")!.GetValue(instance);
                            var result = GetAllRecords(propertyType, propertyTypeName.Name + "Id", val!);

                            instance.GetType().GetProperty(property.Name)!.SetValue(instance, result);
                        }
                    }

                    myList.GetType().GetMethod("Add")!.Invoke(myList, new object[] { instance });
                }
                return myList;
            }
            else
            {
                var data = adoNetUtil.GetById(type.Name, keyName, id);
                var instance = type.GetConstructors().FirstOrDefault()!.Invoke(Array.Empty<object>());

                foreach(var value in data)
                {
                    foreach (var item in value)
                    {
                        var propertyInstance = instance.GetType().GetProperty(item.columnName);
                        if (propertyInstance is not null)
                            propertyInstance.SetValue(instance, item.value);
                    }
                }

                if (data.Count > 0) 
                {
                    foreach (var property in type.GetProperties())
                    {
                        var propertyType = property.PropertyType;
                        if (!IsPrimitive(propertyType))
                        {
                            var val = instance.GetType().GetProperty("Id")!.GetValue(instance);
                            var result = GetAllRecords(propertyType, type.Name + "Id", val!);

                            instance.GetType().GetProperty(property.Name)!.SetValue(instance, result);
                        }
                    }
                }
                return data.Count is 0 ? null : instance;
            }
        }

        private void DeleteRecord(Type entity, (string name, object id) key)
        {
            try
            {
                AdoNetUtililty adoNetUtililty = new();

                foreach(var property in entity
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance))
                 {
                    var entityName = entity.Name;

                    if (property.PropertyType.GetInterfaces().Contains(typeof(IList)))
                    {
                        var propertyType = property.PropertyType.GetGenericArguments().First();

                        foreach(var id in
                            adoNetUtililty.GetPrimaryKey(propertyType.Name, entityName + "Id", key.id))
                        {
                            DeleteRecord(propertyType, ("Id", id));
                        }

                    }

                    else
                    {
                        if (!IsPrimitive(property.PropertyType))
                        {
                            var propertyType = property.PropertyType;
                            foreach(var id in adoNetUtililty
                                .GetPrimaryKey(propertyType.Name, entityName + "Id", key.id))
                                DeleteRecord(propertyType, ("Id", id));
                        }
                    }
                }
                
                adoNetUtililty.DeleteById(entity.Name,key.name, key.id);
            }
            catch(Exception ex) { throw new Exception(ex.Message, ex); }
        }

        private void UpdateRecord(object entity)
        {
            AdoNetUtililty adoNetUtililty = new();
            var dictionary = new Dictionary<string, object>();

            try
            {
                var properties = entity.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach(var property in properties)
                {
                    if (IsPrimitive(property.PropertyType)
                        && property.GetValue(entity) is object value)
                    {
                         dictionary.Add(property.Name, value);
                    }
                }
                var key = properties.Where(x => x.Name == "Id").First().GetValue(entity);
                adoNetUtililty.Update(entity.GetType().Name, dictionary, key!);

                foreach (var property in properties)
                {
                    if (property.PropertyType.GetInterfaces().Contains(typeof(IList)))
                    {
                        if (property.GetValue(entity) is IList items)
                        {
                            foreach (var item in items)
                            {
                                UpdateRecord(item);
                            }
                        }
                    }

                    else
                    {
                        if (!IsPrimitive(property.PropertyType) 
                            && property.GetValue(entity) is object propertyValue)
                        {
                            UpdateRecord(propertyValue);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        private void InsertRecord(object entity, params string[] key)
        {
            var dictionary = new Dictionary<string, object>();
            var utility = new AdoNetUtililty();

            if (key.Length is 2)
                dictionary[key[0]] = key[1];

            try
            {
                var properties = entity.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    if (IsPrimitive(property.PropertyType) 
                        && property.GetValue(entity) is object propertyValue)  
                            dictionary.Add(property.Name, propertyValue);
                }

                utility.Insert(entity.GetType().Name, dictionary);

                foreach (var property in properties)
                {
                    if (property.PropertyType.GetInterfaces().Contains(typeof(IList))
                        && property.GetValue(entity) is IList items)
                    {
                        foreach (var item in items)
                        {
                            var primaryKey = GetEntityPrimaryKey(entity);
                            InsertRecord(item, primaryKey.ColumnName, primaryKey.Value);
                        }
                    }

                    else
                    {
                        if(!IsPrimitive(property.PropertyType) 
                            && property.GetValue(entity) is object propertyValue)
                        {
                            var primaryKey = GetEntityPrimaryKey(entity);
                            InsertRecord(propertyValue, primaryKey.ColumnName, primaryKey.Value);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        private bool IsPrimitive(Type type)
            => type.IsPrimitive || type == typeof(Guid) || type == typeof(string)
               || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type.IsEnum || !type.IsClass
               ? true 
               : false;

        private (string ColumnName,string Value) GetEntityPrimaryKey(object entity)
        {
            string val = entity.GetType().GetProperty("Id")!.GetValue(entity)!.ToString()!;
            string name = entity.GetType()?.Name + "Id";
            return (name, val);
        }
        
    }

    public class AdoNetUtililty
    {
        private SqlConnection SqlConnection = new SqlConnection
            ("Data Source=.\\SQLEXPRESS;Initial Catalog=AspnetB9;User ID=aspnetb9; Password=123456;TrustServerCertificate=True;");

        public void Insert(string entityName, Dictionary<string, object> parameters)
        {
            var paramsName = new StringBuilder();
            var paramsNameWith = new StringBuilder();
            using SqlCommand command = new();

            foreach (var parameter in parameters)
            {
                if (paramsName.Length > 0)
                {
                    paramsName.Append(",");
                    paramsNameWith.Append(",");
                }
                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                paramsName.Append(parameter.Key);
                paramsNameWith.Append("@" + parameter.Key);
            }

            string cmdText = $"Insert into [{entityName}] ({paramsName}) values ({paramsNameWith});";
            command.CommandText = cmdText;
            command.Connection = SqlConnection;
            if (command.Connection.State != System.Data.ConnectionState.Open)
                command.Connection.Open();

            command.ExecuteNonQuery();
            command.Connection.Close();
        }

        public void Update(string entityName, Dictionary<string, object> parameters, object key)
        {
            using SqlCommand command = new();
            StringBuilder cmdText = new();

            foreach (var parameter in parameters)
            {
                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                if (cmdText.Length > 0) cmdText.Append(",");
                cmdText.Append($"{parameter.Key} = @{parameter.Key}");
            }

            string updateStatement = $"UPDATE [{entityName}] SET {cmdText} WHERE Id = '{key}'";
            command.CommandText = updateStatement;
            command.Connection = SqlConnection;
            if (command.Connection.State != System.Data.ConnectionState.Open)
                command.Connection.Open();
            command.ExecuteNonQuery();
            command.Connection.Close();
        }

        public void DeleteById(string tableName, string name, object id)
        {
            using SqlCommand command = new();
            string deleteTxt = $"DELETE FROM [{tableName}] WHERE {name} = '{id}' ";
            command.CommandText = deleteTxt;
            command.Connection = SqlConnection;
            if (command.Connection.State != System.Data.ConnectionState.Open)
                command.Connection.Open();
            command.ExecuteNonQuery();
            command.Connection.Close();
        }

        public IList<(string columnName, object value)[]> GetById(string entityName, string name, object id)
        {
            using SqlCommand command = new();
            string getStatement = $"Select * from [{entityName}] where {name} = '{id}'";
            command.CommandText = getStatement;
            command.Connection = SqlConnection;
            if (command.Connection.State != System.Data.ConnectionState.Open)
                command.Connection.Open();

            var reader = command.ExecuteReader();

            List<(string, object)[]> data = new();

            while (reader.Read())
            {
                (string, object)[] row = new (string, object)[reader.FieldCount];

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[i] = (reader.GetName(i), reader.GetValue(i));
                }

                data.Add(row);
            }
            command.Connection.Close();
            return data;
        }

        public IList<(string columnName, object value)[]> GetAll(string tableName)
        {
            try
            {
                using SqlCommand command = new();
                string getSqlQuery = $"Select * from [{tableName}]";
                command.CommandText = getSqlQuery;
                command.Connection = SqlConnection;
                if (command.Connection.State != System.Data.ConnectionState.Open)
                    command.Connection.Open();

                var reader = command.ExecuteReader();

                List<(string, object)[]> data = new();

                while (reader.Read())
                {
                    (string, object)[] row = new (string, object)[reader.FieldCount];

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[i] = (reader.GetName(i), reader.GetValue(i));
                    }

                    data.Add(row);
                }
                command.Connection.Close();
                return data;
            }
            catch (Exception ex) { throw new Exception(ex.Message, ex); }
        }

        public IList<object> GetPrimaryKey(string tableName, string name, object id)
        {
            using SqlCommand command = new SqlCommand();
            string cmdText = $"Select Id from [{tableName}] where {name} = '{id}'";
            command.CommandText = cmdText;
            command.Connection = SqlConnection;
            if (command.Connection.State != System.Data.ConnectionState.Open)
                command.Connection.Open();

            try
            {
                var reader = command.ExecuteReader();

                List<object> data = new();

                while (reader.Read())
                {
                    object row = new object[reader.FieldCount];

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row = reader.GetValue(i);
                    }

                    data.Add(row);
                }
                command.Connection.Close();
                return data;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }

        }
    }
}
