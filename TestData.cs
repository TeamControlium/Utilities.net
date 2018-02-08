using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamControlium.Utilities
{
    public class TestData
    {
        private static TestDataRepository<dynamic> dataRepository = new TestDataRepository<dynamic>();
        private static Dictionary<string, Dictionary<string, dynamic>> testData = new Dictionary<string, Dictionary<string, dynamic>>();

        /// <summary>
        /// Repository for all Data used within current test
        /// </summary>
        /// <remarks>
        /// Data persists throughout the lifetime of application instantiating Utilities class
        /// </remarks>
        /// <example>
        /// <code language="cs">
        /// //
        /// // Store the string MyString as item named MyName in the TestData
        /// // repository category MyCategory
        /// //
        /// Utilities.TestData.Repository["MyCategory","MyName"] = "MyString";
        /// 
        /// //
        /// // Obtain the item MyName from TestData category MyCategory
        /// //
        /// string myString = Utilities.TestData.Repository["MyCategory","MyName"];
        /// </code>
        /// </example>
        public static TestDataRepository<dynamic> Repository
        {
            get
            {
                return dataRepository;
            }
        }


        /// <summary>
        /// Underlying TestData repository
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public sealed class TestDataRepository<T> // : Dictionary<string, Dictionary<string, dynamic>>
        {
            /// <summary>
            /// Returns the last exception were a TryGetRunCategoryOptions returned false
            /// </summary>
            public Exception TryException { get; private set; }

            /// <summary>
            /// Clears the Test Data repository of all test data
            /// </summary>
            public void Clear()
            {
                Logger.WriteLine(Logger.LogLevels.FrameworkDebug, "Clearing Test Data repository");
                testData.Clear();
            }

            public bool HasCategory(string key)
            {
                return testData.ContainsKey(key);
            }

            /// <summary>
            /// Sets or Gets test data item [name] in category [category]
            /// </summary>
            /// <remarks>
            /// If name and/or category does not exist when setting, item is created.  If item already exists it is updated with new value<br/><br/>
            /// Names of items can be duplicated if in seperate categories but must be unique in a single category<br/>
            /// It is the responsibility of the calling software to ensure correct type-casting on a Get
            /// </remarks>
            /// <param name="category">Category for test data item</param>
            /// <param name="name">Name of item within Category</param>
            /// <returns>Item named from defined category</returns>
            /// <exception cref="System.ArgumentException">Thrown if category and/or name are null/empty when getting</exception>
            /// <exception cref="System.ArgumentOutOfRangeException">Thrown if category does not exist or item named does not exist in defined category when getting</exception>
            public dynamic this[string category, string name]
            {
                get
                {
                    // Ensure name is valid - not null or empty and named category contains it
                    if (string.IsNullOrEmpty(name))
                        throw new ArgumentException(string.Format("Cannot be null or empty ({0})", name == null ? "Is Null" : "Is empty"), "name");
                    if (!this[category].ContainsKey(name))
                        throw new ArgumentOutOfRangeException("name", name, string.Format("Category [{0}] does not have item named", category));
                    // Get item named from category and return it
                    dynamic obtainedObject = (this[category])[name];
                    // Do logging
                    Logger.Write(Logger.LogLevels.FrameworkDebug, "Got ");
                    if (obtainedObject is string)
                    {
                        Logger.Write(Logger.LogLevels.FrameworkDebug, "string [{0}]", ((string)obtainedObject)?.Length < 50 ? (string)obtainedObject : (((string)obtainedObject).Substring(0, 47) + "...") ?? "");
                    }
                    else if (obtainedObject is int)
                    {
                        Logger.Write(Logger.LogLevels.FrameworkDebug, "integer [{0}]", ((int)obtainedObject));
                    }
                    else if (obtainedObject is float)
                    {
                        Logger.Write(Logger.LogLevels.FrameworkDebug, "integer [{0}]", ((float)obtainedObject));
                    }
                    else
                    {
                        Logger.Write(Logger.LogLevels.FrameworkDebug, "type {0}{1}", obtainedObject.ToString(), (obtainedObject == null) ? " (is null!)" : "");
                    }
                    Logger.WriteLine(Logger.LogLevels.FrameworkDebug, " from [{0}][{1}]", category, name);
                    return obtainedObject;
                }
                set
                {
                    Logger.Write(Logger.LogLevels.FrameworkDebug, "Setting [{0}][{1}] to ", category, name);
                    if (value is string)
                    {
                        Logger.Write(Logger.LogLevels.FrameworkDebug, "string [{0}]", ((string)value)?.Length < 50 ? (string)value : (((string)value).Substring(0, 47) + "...") ?? "");
                    }
                    else if (value is int)
                    {
                        Logger.Write(Logger.LogLevels.FrameworkDebug, "integer [{0}]", ((int)value));
                    }
                    else if (value is float)
                    {
                        Logger.Write(Logger.LogLevels.FrameworkDebug, "integer [{0}]", ((float)value));
                    }
                    else
                    {
                        Logger.Write(Logger.LogLevels.FrameworkDebug, "type {0}{1}", value.ToString(), (value == null) ? " (is null!)" : "");
                    }
                    // Add Category if we dont already have it
                    if (!testData.ContainsKey(category))
                        testData.Add(category, new Dictionary<string, dynamic>());
                    // Select Category
                    Dictionary<string, dynamic> wholeCategory = testData[category];
                    // Add Name if we dont already have it in the current category, otherwise change contents of name
                    if (wholeCategory.ContainsKey(name))
                        wholeCategory[name] = value;
                    else
                        wholeCategory.Add(name, value);
                }
            }

            /// <summary>
            /// Gets all items in category
            /// </summary>
            /// <param name="category">Category to return</param>
            /// <returns>Dictionary of all items in named category</returns>
            /// <exception cref="System.ArgumentException">Thrown if category argument is null/empty</exception>
            /// <exception cref="System.ArgumentOutOfRangeException">Thrown if category does not exist</exception>
            public new Dictionary<string, dynamic> this[string category]
            {
                get
                {
                    if (string.IsNullOrEmpty(category))
                        throw new ArgumentException(string.Format("Cannot be null or empty ({0})", category == null ? "Is Null" : "Is empty"), "Category");
                    if (!testData.ContainsKey(category))
                        throw new ArgumentOutOfRangeException("category", category, "Category does not exist");
                    var wholeCategory = testData[category];
                    Logger.WriteLine(Logger.LogLevels.FrameworkDebug, "Got category [{0}] ({1} items)", category, wholeCategory.Count);
                    return wholeCategory;
                }
                set
                {
                    if (string.IsNullOrEmpty(category))
                        throw new ArgumentException(string.Format("Cannot be null or empty ({0})", category == null ? "Is Null" : "Is empty"), "Category");
                    // Add Category if we dont already have it
                    if (!testData.ContainsKey(category))
                        testData.Add(category, new Dictionary<string, dynamic>());
                    Logger.WriteLine(Logger.LogLevels.FrameworkDebug, "Setting category [{0}] ({1} items)", category, value.Count);
                    testData[category] = value;
                }
            }

            /// <summary>Returns named item from named category</summary>
            /// <param name="category">Category to obtain option from</param>
            /// <param name="name">Name of option to get</param>
            /// <typeparam name="U">Expected return Type of object being obtained</typeparam>
            /// <returns>Object of Category and Name</returns>
            public U GetItem<U>(string category, string name)
            {
                try
                {
                    dynamic obtainedObject = this[category, name];

                    if (obtainedObject is U)
                    {
                        return (U)obtainedObject;
                    }
                    else if (obtainedObject != null && (typeof(U) == typeof(int) && obtainedObject.GetType().Equals(typeof(string))))
                    {
                        // We want and int and we got a string.  So, try converting it to be helpful....
                        try
                        {
                            return (U)int.Parse(obtainedObject);
                        }
                        catch { }
                    }
                    else if (obtainedObject != null && (typeof(U) == typeof(float) && obtainedObject.GetType().Equals(typeof(string))))
                    {
                        // We want and float and we got a string.  So, try converting it to be helpful....
                        try
                        {
                            return (U)float.Parse(obtainedObject);
                        }
                        catch { }
                    }

                    throw new Exception(string.Format("Expected type [{0}] but got type [{1}].", typeof(U).Name, obtainedObject.GetType()));
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Exception getting Category.Name ([{0}].[{1}])", category, name), ex);
                }
            }


            /// <summary>Returns named option from named category</summary>
            /// <param name="category">Category to obtain option from</param>
            /// <param name="name">Name of option to get</param>
            /// <param name="value">Object of Category and Name if successful</param>
            /// <typeparam name="U">Expected return Type of object being obtained</typeparam>
            /// <returns>true if object obtained successfullt otherwise false (use TryExeception to exception thrown)</returns>
            public bool TryGetItem<U>(string category, string name, out U value)
            {
                try
                {
                    value = GetItem<U>(category, name);
                    return true;
                }
                catch (Exception ex)
                {
                    TryException = new Exception(string.Format("Exception getting Category.Name ([{0}].[{1}])", category, name), ex);
                    value = default(U);
                    return false;
                }
            }

            /// <summary>Returns named option from named category.  Returns null if object not set</summary>
            /// <param name="category">Category to obtain option from</param>
            /// <param name="name">Name of option to get</param>
            /// <typeparam name="U">Expected return Type of object being obtained</typeparam>
            /// <returns>Object of Category and Name.  If object does not exist returns default value (Null if a reference type)</returns>
            public U GetItemOrDefault<U>(string category, string name)
            {
                dynamic obtainedObject;
                try
                {
                    obtainedObject = this[category, name];
                }
                catch
                {
                    Logger.WriteLine(Logger.LogLevels.FrameworkInformation, "No data found for [{0}.{1}] - returning [Type ({2})] default value.", category, name, typeof(U).Name);
                    obtainedObject = default(U);
                }

                if (obtainedObject is U)
                    return (U)obtainedObject;
                else
                    throw new InvalidCastException(string.Format("Expected type [{0}] but got type [{1}].", typeof(U).Name, obtainedObject.GetType().Name));
            }

            /// <summary>Returns all options defined in passed Category</summary>
            /// <param name="Category">Category to get options for</param>
            /// <returns>Dictionary of KeyValue pairs where Keys are the option names and Values are the option data</returns>
            public Dictionary<string, dynamic> GetCategoryItems(string Category)
            {
                try
                {
                    return testData[Category];
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Exception getting itms in Category ([{0}])", Category), ex);
                }
            }

        }
    }

}
