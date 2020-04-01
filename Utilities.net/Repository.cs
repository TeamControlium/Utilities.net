// <copyright file="Repository.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using static Log;

    /// <summary>
    /// Central Test Data repository for tool independent storage of data used within tests.  TeamControlium repository is intended to be used as the primary mechanism for storage of
    /// data within a Test Framework and is used for configuration settings of the Controlium based library.  It is a thread-safe static repository with dynamic objects that can be
    /// scoped for global (all threads) or local (local thread only visibility) use but with the ability to clone data from Global to Local if/as required.
    /// </summary>
    public class Repository
    {
        /// <summary>
        /// Actual storage of data.  Three dimensional dictionary.
        /// Dimension 1 is the ThreadID storing data for the identified specific thread.
        /// Dimension 2 is the data categoryName
        /// Dimension 3 is the data item name
        /// Data is stored as dynamic objects enabling storage of anything and runtime changes of item location types as well as varying types in the repository.
        /// </summary>
        private static Dictionary<int, Dictionary<string, Dictionary<string, dynamic>>> repository = new Dictionary<int, Dictionary<string, Dictionary<string, dynamic>>>();

        /// <summary>
        /// When data is stored in the Global repository (IE. Seen by all threads) the threadID used is -1.
        /// </summary>
        private static int globalIndex = -1; // Index of Global TestData (IE. Test Data available to all threads)

        /// <summary>
        /// Repository allows test code to store data singly or in Categories.  If storing a data item without a Category, it actually goes in the categoryName named in this variable. 
        /// </summary>
        /// <remarks>There is the chance a test framework could create a categoryName with the same name.  However, this a low risk and could always be changed at a later date if needed.</remarks>
        private static string categorylessItems = "NoCategoryName";

        /// <summary>
        /// Stores the last exception/s caught when using Try... methods.  Is in a dictionary to enable thread-safe and thread oriented exception storage.  A thread can ONLY see the
        /// last exception thrown for it's specific thread.  The ensures maximum flexibility in parallel testing scenarios.
        /// </summary>
        private static Dictionary<int, object> lastException = new Dictionary<int, object>();

        /// <summary>
        /// Gets reference to the Dynamic items class allowing indexed access to and setting of all Local stored data items in current thread.
        /// </summary>
        /// <remarks>This is a reference to an indexer class.  So can be used as an indexed property.  All data stored in the Local repository are visible only to code executing within the current thread.</remarks>
        /// <example>Example stores string 'some data' in the repository, naming it 'MyItem'.
        /// Then recalls Repository item 'MyItem', storing it in myData.<code>
        /// Repository.ItemLocal["MyItem"] = "some data";
        /// string myData = Repository.ItemLocal["MyItem"]; // myData now equal to 'some data'
        /// </code></example>
        public static DynamicItems<dynamic> ItemLocal { get; } = new DynamicItems<dynamic>(true);

        /// <summary>
        /// Gets reference to the Dynamic items class allowing indexed access to and setting of all Globally stored data items.
        /// </summary>
        /// <remarks>This is a reference to an indexer class.  So can be used as an indexed property.  All data stored in the Global repository are visible across all threads.</remarks>
        /// <example>Example stores string 'some data' in the repository, naming it 'MyItem'.
        /// Then recalls Repository item 'MyItem', storing it in myData.<code>
        /// Repository.ItemGlobal["MyItem"] = "some data";
        /// string myData = Repository.ItemGlobal["MyItem"]; // myData now equal to 'some data'
        /// </code></example>
        public static DynamicItems<dynamic> ItemGlobal { get; } = new DynamicItems<dynamic>(false);

        /// <summary>
        /// Returns the last exception were a TryGetRunCategoryOptions returned false
        /// </summary>
        /// <returns>Last exception thrown by a Try... method in this thread.</returns>
        /// <remarks>Return is cast as Exception but underlying type can be obtained by called (IE. <code>var myType = Repository.RepositoryLastTryException().GetType();</code></remarks>
        public static Exception RepositoryLastTryException()
        {
            int threadID = Thread.CurrentThread.ManagedThreadId;
            if (lastException.ContainsKey(threadID))
            {
                return (Exception)lastException[threadID];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Indicates whether Local repository has a Category with the given name
        /// </summary>
        /// <param name="categoryName">Name of category to check for.</param>
        /// <returns>True if local repository has category named.  Otherwise False.</returns>
        public static bool HasCategoryLocal(string categoryName)
        {
            lock (repository)
            {
                return verifyCategoryIsNotNullOrEmptyAndExists(false, categoryName, false);
            }
        }

        /// <summary>
        /// Indicates whether Global repository has a Category with the given name
        /// </summary>
        /// <param name="categoryName">Name of category to check for.</param>
        /// <returns>True if global repository has category named.  Otherwise False.</returns>
        public static bool HasCategoryGlobal(string categoryName)
        {
            return verifyCategoryIsNotNullOrEmptyAndExists(false, categoryName, false);
        }

        /// <summary>
        /// Returns all items (in dictionary) from required local category
        /// </summary>
        /// <param name="categoryName">Name of category to retrieve items from</param>
        /// <returns>All items from Local repository in named Category.</returns>
        public static Dictionary<string, dynamic> GetCategoryLocal(string categoryName)
        {
            return getCategory(true, categoryName);
        }

        /// <summary>
        /// Returns all items (in dictionary) from required global category
        /// </summary>
        /// <param name="categoryName">Name of category to retrieve items from</param>
        /// <returns>All items from Global repository in named Category.</returns>
        public static Dictionary<string, dynamic> GetCategoryGlobal(string categoryName)
        {
            return getCategory(false, categoryName);
        }

        /// <summary>
        /// Gets all items (in dictionary) from required local category indicating success or failure of retrieval
        /// </summary>
        /// <param name="categoryName">Name of category to retrieve items from</param>
        /// <param name="category">Output parameter populated with all items in named local category if successful.  Null reference if not</param>
        /// <returns>True if Category exists and item/s retrieved successfully.  False if not.</returns>
        /// <remarks>If method fails and returns False, <see cref="RepositoryLastTryException"/> can be used to obtain exception thrown.</remarks>
        public static bool TryGetCategoryLocal(string categoryName, out Dictionary<string, dynamic> category)
        {
            try
            {
                category = getCategory(true, categoryName);
                return true;
            }
            catch (Exception ex)
            {
                category = null;
                lastException.Add(Thread.CurrentThread.ManagedThreadId, ex);
                return false;
            }
        }

        /// <summary>
        /// Gets all items (in dictionary) from required global category indicating success or failure of retrieval
        /// </summary>
        /// <param name="categoryName">Name of category to retrieve items from</param>
        /// <param name="category">Output parameter populated with all items in named global category if successful.  Null reference if not</param>
        /// <returns>True if Category exists and item/s retrieved successfully.  False if not.</returns>
        /// <remarks>If method fails and returns False, <see cref="RepositoryLastTryException"/> can be used to obtain exception thrown.</remarks>
        public static bool TryGetCategoryGlobal(string categoryName, out Dictionary<string, dynamic> category)
        {
            try
            {
                category = getCategory(false, categoryName);
                return true;
            }
            catch (Exception ex)
            {
                category = null;
                lastException.Add(globalIndex, ex);
                return false;
            }
        }

        /// <summary>
        /// Load named Local repository category with required data.
        /// </summary>
        /// <remarks>If category already exists, merge data optionally overwriting if duplicate keys found.</remarks>
        /// <param name="categoryName">Name of category to add or merge in</param>
        /// <param name="category">Category data to add or merge in</param>
        /// <param name="overwriteExistingItems">If true, any existing data items are overwritten.  Otherwise any duplicates will result in an Exception.</param>
        public static void SetCategoryLocal(string categoryName, Dictionary<string, dynamic> category, bool overwriteExistingItems)
        {
            setCategory(true, categoryName, category, overwriteExistingItems);
        }

        /// <summary>
        /// Load named Global repository category with required data.
        /// </summary>
        /// <remarks>If category already exists, merge data optionally overwriting if duplicate keys found.</remarks>
        /// <param name="categoryName">Name of category to add or merge in</param>
        /// <param name="category">Category data to add or merge in</param>
        /// <param name="overwriteExistingItems">If true, any existing data items are overwritten.  Otherwise any duplicates will result in an Exception.</param>
        public static void SetCategoryGlobal(string categoryName, Dictionary<string, dynamic> category, bool overwriteExistingItems)
        {
            setCategory(false, categoryName, category, overwriteExistingItems);
        }

        /// <summary>
        /// Returns non-categorized data item from local repository.
        /// </summary>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <remarks>If item does not exist or there is a error obtaining item an exception will be thrown</remarks>
        /// <returns>Data item.  Type is dynamic so it is callers responsibility to ensure correct typing (Use <see cref="GetItemLocal{T}(string)"/> for type checked data recall.</returns>
        public static dynamic GetItemLocal(string itemName)
        {
            lock (repository)
            {
                return getItem(true, categorylessItems, itemName);
            }
        }

        /// <summary>
        /// Returns non-categorized data item from global repository.
        /// </summary>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <remarks>If item does not exist or there is a error obtaining item an exception will be thrown</remarks>
        /// <returns>Data item.  Type is dynamic so it is callers responsibility to ensure correct typing (Use <see cref="GetItemGlobal{T}(string)"/> for type checked data recall.</returns>
        public static dynamic GetItemGlobal(string itemName)
        {
            lock (repository)
            {
                return getItem(false, categorylessItems, itemName);
            }
        }

        /// <summary>
        /// Returns data item from required category in local repository.
        /// </summary>
        /// <param name="categoryName">Name of category data item is in</param>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <returns>Data item.  Type is dynamic so it is callers responsibility to ensure correct typing (Use <see cref="GetItemLocal{T}(string, string)"/> for type checked data recall.</returns>
        public static dynamic GetItemLocal(string categoryName, string itemName)
        {
            lock (repository)
            {
                return getItem(true, categoryName, itemName);
            }
        }

        /// <summary>
        /// Returns data item from required category in global repository.
        /// </summary>
        /// <param name="categoryName">Name of category data item is in</param>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <returns>Data item.  Type is dynamic so it is callers responsibility to ensure correct typing (Use <see cref="GetItemGlobal{T}(string, string)"/> for type checked data recall.</returns>
        public static dynamic GetItemGlobal(string categoryName, string itemName)
        {
            lock (repository)
            {
                return getItem(false, categoryName, itemName);
            }
        }

        /// <summary>
        /// Returns non-categorized data item from local repository with verification of required type
        /// </summary>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <typeparam name="T">Expected type of required data</typeparam>
        /// <remarks>If item does not exist, there is a error obtaining item or the type is not of the required type an exception will be thrown</remarks>
        /// <returns>Data item.  Type will be cast as required.</returns>
        public static T GetItemLocal<T>(string itemName)
        {
            lock (repository)
            {
                return getItemTyped<T>(true, categorylessItems, itemName);
            }
        }

        /// <summary>
        /// Returns non-categorized data item from global repository with verification of required type
        /// </summary>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <typeparam name="T">Expected type of required data</typeparam>
        /// <remarks>If item does not exist, there is a error obtaining item or the type is not of the required type an exception will be thrown</remarks>
        /// <returns>Data item.  Type will be cast as required.</returns>
        public static T GetItemGlobal<T>(string itemName)
        {
            lock (repository)
            {
                return getItemTyped<T>(false, categorylessItems, itemName);
            }
        }

        /// <summary>
        /// Returns data item from required category in local repository with verification of required type.
        /// </summary>
        /// <param name="categoryName">Name of category data item is in</param>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <typeparam name="T">Expected type of required data</typeparam>
        /// <remarks>If item does not exist, there is a error obtaining item or the type is not of the required type an exception will be thrown</remarks>
        /// <returns>Data item.  Type will be cast as required.</returns>
        public static T GetItemLocal<T>(string categoryName, string itemName)
        {
            lock (repository)
            {
                return getItemTyped<T>(true, categoryName, itemName);
            }
        }

        /// <summary>
        /// Returns data item from required category in global repository with verification of required type.
        /// </summary>
        /// <param name="categoryName">Name of category data item is in</param>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <typeparam name="T">Expected type of required data</typeparam>
        /// <remarks>If item does not exist, there is a error obtaining item or the type is not of the required type an exception will be thrown</remarks>
        /// <returns>Data item.  Type will be cast as required.</returns>
        public static T GetItemGlobal<T>(string categoryName, string itemName)
        {
            lock (repository)
            {
                return getItemTyped<T>(false, categoryName, itemName);
            }
        }

        /// <summary>
        /// Returns non-categorized data item from local repository indicating if successful or not
        /// </summary>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <param name="item">Item found (if succesful) or null. Type is dynamic so it is callers responsibility to ensure correct typing (Use <see cref="TryGetItemLocal{T}(string,T)"/> for type checked data recall.</param>
        /// <returns>True if sucessful, False if not.</returns>
        /// <remarks>If method fails and returns False, <see cref = "RepositoryLastTryException" /> can be used to obtain exception thrown.</remarks>
        public static bool TryGetItemLocal(string itemName, out dynamic item)
        {
            lock (repository)
            {
                try
                {
                    item = getItem(true, categorylessItems, itemName);
                    return true;
                }
                catch (Exception ex)
                {
                    SetLastException(ex);
                    item = null;
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns non-categorized data item from global repository indicating if successful or not.
        /// </summary>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <param name="item">Item found (if succesful) or null. Type is dynamic so it is callers responsibility to ensure correct typing (Use <see cref = "TryGetItemGlobal{T}(string,T)"/> for type checked data recall).</param>
        /// <returns>True if sucessful, False if not.</returns>
        /// <remarks>If method fails and returns False, <see cref = "RepositoryLastTryException" /> can be used to obtain exception thrown.</remarks>
        public static bool TryGetItemGlobal(string itemName, out dynamic item)
        {
            lock (repository)
            {
                try
                {
                    item = getItem(false, categorylessItems, itemName);
                    return true;
                }
                catch (Exception ex)
                {
                    SetLastException(ex);
                    item = null;
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns data item from required category in local repository with verification of required type indicating if successful or not.
        /// </summary>
        /// <param name="categoryName">Name of category data item is in</param>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <param name="item">Item found (if succesful) or null. Type is dynamic so it is callers responsibility to ensure correct typing (Use <see cref="TryGetItemLocal{T}(string,string,T)"/> for type checked data recall.</param>
        /// <returns>If method fails and returns False, <see cref = "RepositoryLastTryException" /> can be used to obtain exception thrown.</returns>
        public static bool TryGetItemLocal(string categoryName, string itemName, out dynamic item)
        {
            lock (repository)
            {
                try
                {
                    item = getItem(true, categoryName, itemName);
                    return true;
                }
                catch (Exception ex)
                {
                    SetLastException(ex);
                    item = null;
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns data item from required category in global repository with verification of required type indicating if successful or not.
        /// </summary>
        /// <param name="categoryName">Name of category data item is in</param>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <param name="item">Item found (if succesful) or null. Type is dynamic so it is callers responsibility to ensure correct typing (Use <see cref="TryGetItemGlobal{T}(string,string,T)"/> for type checked data recall.</param>
        /// <returns>If method fails and returns False, <see cref = "RepositoryLastTryException" /> can be used to obtain exception thrown.</returns>
        public static bool TryGetItemGlobal(string categoryName, string itemName, out dynamic item)
        {
            lock (repository)
            {
                try
                {
                    item = getItem(false, categoryName, itemName);
                    return true;
                }
                catch (Exception ex)
                {
                    SetLastException(ex);
                    item = null;
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns non-categorized data item from local repository with verification of required type indicating if successful or not
        /// </summary>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <param name="item">Item found (if succesful) or null. Type checked with expected type to ensure type compatibility.</param>
        /// <typeparam name="T">Expected type of data item</typeparam>
        /// <returns>True if sucessful, False if not or type differs to expected.</returns>
        /// <remarks>If method fails and returns False, <see cref = "RepositoryLastTryException" /> can be used to obtain exception thrown.</remarks>
        public static bool TryGetItemLocal<T>(string itemName, out T item)
        {
            lock (repository)
            {
                try
                {
                    item = getItemTyped<T>(true, categorylessItems, itemName);
                    return true;
                }
                catch (Exception ex)
                {
                    SetLastException(ex);
                    item = default(T);
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns non-categorized data item from global repository with verification of required type indicating if successful or not
        /// </summary>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <param name="item">Item found (if succesful) or null. Type checked with expected type to ensure type compatibility.</param>
        /// <typeparam name="T">Expected type of data item</typeparam>
        /// <returns>True if sucessful, False if not or type differs to expected.</returns>
        /// <remarks>If method fails and returns False, <see cref = "RepositoryLastTryException" /> can be used to obtain exception thrown.</remarks>
        public static bool TryGetItemGlobal<T>(string itemName, out T item)
        {
            lock (repository)
            {
                try
                {
                    item = getItemTyped<T>(false, categorylessItems, itemName);
                    return true;
                }
                catch (Exception ex)
                {
                    SetLastException(ex);
                    item = default(T);
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns categorized data item from local repository with verification of required type indicating if successful or not
        /// </summary>
        /// <param name="categoryName">Name of category to retreive data item from</param>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <param name="item">Item found (if succesful) or null. Type checked with expected type to ensure type compatibility.</param>
        /// <typeparam name="T">Expected type of data item</typeparam>
        /// <returns>True if sucessful, False if not or type differs to expected.</returns>
        /// <remarks>If method fails and returns False, <see cref = "RepositoryLastTryException" /> can be used to obtain exception thrown.</remarks>
        public static bool TryGetItemLocal<T>(string categoryName, string itemName, out T item)
        {
            lock (repository)
            {
                try
                {
                    item = getItemTyped<T>(true, categoryName, itemName);
                    return true;
                }
                catch (Exception ex)
                {
                    SetLastException(ex);
                    item = default(T);
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns categorized data item from global repository with verification of required type indicating if successful or not
        /// </summary>
        /// <param name="categoryName">Name of category to retreive data item from</param>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <param name="item">Item found (if succesful) or null. Type checked with expected type to ensure type compatibility.</param>
        /// <typeparam name="T">Expected type of data item</typeparam>
        /// <returns>True if sucessful, False if not or type differs to expected.</returns>
        /// <remarks>If method fails and returns False, <see cref = "RepositoryLastTryException" /> can be used to obtain exception thrown.</remarks>

        public static bool TryGetItemGlobal<T>(string categoryName, string itemName, out T item)
        {
            lock (repository)
            {
                try
                {
                    item = getItemTyped<T>(false, categoryName, itemName);
                    return true;
                }
                catch (Exception ex)
                {
                    SetLastException(ex);
                    item = default(T);
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns non-categorized data item from local repository with verification of required type or types default value if item does not exist
        /// </summary>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <typeparam name="T">Expected type of required data</typeparam>
        /// <remarks>If item does not exist, the default value for the Type defied in the call will be returned.</remarks>
        /// <returns>Data item or default value based on Type.  Type will be cast as required.</returns>
        public static T GetItemOrDefaultLocal<T>(string itemName)
        {
            lock (repository)
            {
                try
                {
                    lock (repository)
                    {
                        return getItemTyped<T>(true, categorylessItems, itemName);
                    }
                }
                catch
                {
                    return default(T);
                }
            }
        }

        /// <summary>
        /// Returns non-categorized data item from global repository with verification of required type or types default value if item does not exist
        /// </summary>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <typeparam name="T">Expected type of required data</typeparam>
        /// <remarks>If item does not exist, the default value for the Type defied in the call will be returned.</remarks>
        /// <returns>Data item or default value based on Type.  Type will be cast as required.</returns>
        public static T GetItemOrDefaultGlobal<T>(string itemName)
        {
            lock (repository)
            {
                try
                {
                    lock (repository)
                    {
                        return getItemTyped<T>(false, categorylessItems, itemName);
                    }
                }
                catch
                {
                    return default(T);
                }
            }
        }

        /// <summary>
        /// Returns categorized data item from local repository with verification of required type or types default value if item does not exist
        /// </summary>
        /// <param name="categoryName">Name of Category data item is in</param>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <typeparam name="T">Expected type of required data</typeparam>
        /// <remarks>If item does not exist, the default value for the Type defied in the call will be returned.</remarks>
        /// <returns>Data item or default value based on Type.  Type will be cast as required.</returns>
        public static T GetItemOrDefaultLocal<T>(string categoryName, string itemName)
        {
            lock (repository)
            {
                try
                {
                    lock (repository)
                    {
                        return getItemTyped<T>(true, categoryName, itemName);
                    }
                }
                catch
                {
                    return default(T);
                }
            }
        }

        /// <summary>
        /// Returns categorized data item from global repository with verification of required type or types default value if item does not exist
        /// </summary>
        /// <param name="categoryName">Name of Category data item is in</param>
        /// <param name="itemName">Name of data item to retrieve</param>
        /// <typeparam name="T">Expected type of required data</typeparam>
        /// <remarks>If item does not exist, the default value for the Type defied in the call will be returned.</remarks>
        /// <returns>Data item or default value based on Type.  Type will be cast as required.</returns>
        public static T GetItemOrDefaultGlobal<T>(string categoryName, string itemName)
        {
            lock (repository)
            {
                try
                {
                    lock (repository)
                    {
                        return getItemTyped<T>(false, categoryName, itemName);
                    }
                }
                catch
                {
                    return default(T);
                }
            }
        }

        /// <summary>
        /// Stores data item in local repository with defined non-categorized name
        /// </summary>
        /// <param name="itemName">Label to store data item with</param>
        /// <param name="item">Data item to store</param>
        /// <remarks>If data item already exists it is overwritten with current data</remarks>
        public static void SetItemLocal(string itemName,dynamic item)
        {
            setItem(true, categorylessItems, itemName, item);
        }

        /// <summary>
        /// Stores data item in global repository with defined non-categorized name
        /// </summary>
        /// <param name="itemName">Label to store data item with</param>
        /// <param name="item">Data item to store</param>
        /// <remarks>If data item already exists it is overwritten with current data</remarks>
        public static void SetItemGlobal(string itemName, dynamic item)
        {
            setItem(false, categorylessItems, itemName, item);
        }

        /// <summary>
        /// Stores data item in local repository with defined categorized name
        /// </summary>
        /// <param name="categoryName">Name of category to store data item in</param>
        /// <param name="itemName">Label to store data item with</param>
        /// <param name="item">Data item to store</param>
        /// <remarks>
        /// If category does not exist it is created.
        /// If data item already exists it is overwritten with current data.</remarks>
        public static void SetItemLocal(string categoryName, string itemName, dynamic item)
        {
            setItem(true, categoryName, itemName, item);
        }

        /// <summary>
        /// Stores data item in global repository with defined categorized name
        /// </summary>
        /// <param name="categoryName">Name of category to store data item in</param>
        /// <param name="itemName">Label to store data item with</param>
        /// <param name="item">Data item to store</param>
        /// <remarks>
        /// If category does not exist it is created.
        /// If data item already exists it is overwritten with current data.</remarks>
        public static void SetItemGlobal(string categoryName, string itemName, dynamic item)
        {
            setItem(false, categoryName, itemName, item);
        }

        /// <summary>
        /// Copy all data from Local to Global repository.
        /// </summary>
        /// <param name="overwriteExistingItems">Indicates if existing data should be overwritten.  An exception is thrown if false and any existing item/s would be overwritten.</param>
        /// <remarks>A deep clone is performed to copy items.  However, it should be noted that complex data items (EG. IO refeferences etc) may clone incorrectly.  Complex type cloning should be verified during test framework development to ensure correct cloning.</remarks>
        public static void CloneLocalToGlobal(bool overwriteExistingItems)
        {
            CloneTestData(true, overwriteExistingItems);
        }

        /// <summary>
        /// Copy all data from required Local category to Global repository.
        /// </summary>
        /// <param name="categoryName">Name of category to be copied.</param>
        /// <param name="overwriteExistingItems">Indicates if existing data should be overwritten.  An exception is thrown if false and any existing item/s would be overwritten.</param>
        /// <remarks>A deep clone is performed to copy items.  However, it should be noted that complex data items (EG. IO refeferences etc) may clone incorrectly.  Complex type cloning should be verified during test framework development to ensure correct cloning.</remarks>
        public static void CloneCategoryLocalToGlobal(string categoryName,bool overwriteExistingItems)
        {
            CloneTestDataCategory(true, categoryName, categoryName, overwriteExistingItems);
        }

        /// <summary>
        /// Copy named data item from required Local category to Global repository.
        /// </summary>
        /// <param name="categoryName">Name of category to be copied.</param>
        /// <param name="itemName">Name of item to clone.</param>
        /// <param name="overwriteExistingItems">Indicates if existing data item should be overwritten if it already exists.  An exception is thrown if false and the named data item exists in global repository.</param>
        /// <remarks>A deep clone is performed to copy items.  However, it should be noted that complex data items (EG. IO refeferences etc) may clone incorrectly.  Complex type cloning should be verified during test framework development to ensure correct cloning.</remarks>
        public static void CloneItemLocalToGlobal(string categoryName, string itemName, bool overwriteExistingItems)
        {
            CloneTestDataItem(true, categoryName, itemName, categoryName, itemName, overwriteExistingItems);
        }

        /// <summary>
        /// Copy all data from Local to Global repository indicting if successful or not.
        /// </summary>
        /// <param name="overwriteExistingItems">Indicates if existing data should be overwritten.  An exception is thrown if false and any existing item/s would be overwritten.</param>
        /// <returns>True if successful clone or false if not (See <see cref="RepositoryLastTryException"/> for exception thrown if  false returned).</returns>
        /// <remarks>A deep clone is performed to copy items.  However, it should be noted that complex data items (EG. IO refeferences etc) may clone incorrectly.  Complex type cloning should be verified during test framework development to ensure correct cloning.</remarks>
        public static bool TryCloneLocalToGlobal(bool overwriteExistingItems)
        {
            try
            {
                CloneTestData(true, overwriteExistingItems);
                return true;
            }
            catch (Exception ex)
            {
                lastException[Thread.CurrentThread.ManagedThreadId] = ex;
                return false;
            }
        }

        /// <summary>
        /// Copy all data from required Local category to Global repository indicting if successful or not.
        /// </summary>
        /// <param name="categoryName">Name of category to be copied.</param>
        /// <param name="overwriteExistingItems">Indicates if existing data should be overwritten.  An exception is thrown if false and any existing item/s would be overwritten.</param>
        /// <returns>True if successful clone or false if not (See <see cref="RepositoryLastTryException"/> for exception thrown if  false returned).</returns>
        /// <remarks>A deep clone is performed to copy items.  However, it should be noted that complex data items (EG. IO refeferences etc) may clone incorrectly.  Complex type cloning should be verified during test framework development to ensure correct cloning.</remarks>
        public static bool TryCloneCategoryLocalToGlobal(string categoryName, bool overwriteExistingItems)
        {
            try
            {
                CloneTestDataCategory(true, categoryName, categoryName, overwriteExistingItems);
                return true;
            }
            catch (Exception ex)
            {
                lastException[Thread.CurrentThread.ManagedThreadId] = ex;
                return false;
            }
        }

        /// <summary>
        /// Copy named data item from required Local category to Global repository indicating if successful
        /// </summary>
        /// <param name="categoryName">Name of category to be copied.</param>
        /// <param name="itemName">Name of item to clone.</param>
        /// <param name="overwriteExistingItems">Indicates if existing data item should be overwritten if it already exists.  An exception is thrown if false and the named data item exists in global repository.</param>
        /// <returns>True if successful clone or false if not (See <see cref="RepositoryLastTryException"/> for exception thrown if  false returned).</returns>
        /// <remarks>A deep clone is performed to copy items.  However, it should be noted that complex data items (EG. IO refeferences etc) may clone incorrectly.  Complex type cloning should be verified during test framework development to ensure correct cloning.</remarks>
        public static bool TryCloneItemLocalToGlobal(string categoryName, string itemName, bool overwriteExistingItems)
        {
            try
            {
                CloneTestDataItem(true, categoryName, itemName, categoryName, itemName, overwriteExistingItems);
                return true;
            }
            catch (Exception ex)
            {
                lastException[Thread.CurrentThread.ManagedThreadId] = ex;
                return false;
            }
        }

        /// <summary>
        /// Copy all data from Local to Global repository.
        /// </summary>
        /// <param name="overwriteExistingItems">Indicates if existing data should be overwritten.  An exception is thrown if false and any existing item/s would be overwritten.</param>
        /// <remarks>A deep clone is performed to copy items.  However, it should be noted that complex data items (EG. IO refeferences etc) may clone incorrectly.  Complex type cloning should be verified during test framework development to ensure correct cloning.</remarks>
        public static void CloneGlobalToLocal(bool overwriteExistingItems)
        {
            CloneTestData(false, overwriteExistingItems);
        }

        /// <summary>
        /// Copy all data from required Local category to Global repository.
        /// </summary>
        /// <param name="categoryName">Name of category to be copied.</param>
        /// <param name="overwriteExistingItems">Indicates if existing data should be overwritten.  An exception is thrown if false and any existing item/s would be overwritten.</param>
        /// <remarks>A deep clone is performed to copy items.  However, it should be noted that complex data items (EG. IO refeferences etc) may clone incorrectly.  Complex type cloning should be verified during test framework development to ensure correct cloning.</remarks>
        public static void CloneCategoryGlobalToLocal(string categoryName, bool overwriteExistingItems)
        {
            CloneTestDataCategory(false, categoryName, categoryName, overwriteExistingItems);
        }

        /// <summary>
        /// Copy named data item from required Local category to Global repository.
        /// </summary>
        /// <param name="categoryName">Name of category to be copied.</param>
        /// <param name="itemName">Name of item to clone.</param>
        /// <param name="overwriteExistingItems">Indicates if existing data item should be overwritten if it already exists.  An exception is thrown if false and the named data item exists in global repository.</param>
        /// <remarks>A deep clone is performed to copy items.  However, it should be noted that complex data items (EG. IO refeferences etc) may clone incorrectly.  Complex type cloning should be verified during test framework development to ensure correct cloning.</remarks>
        public static void CloneItemGlobalToLocal(string categoryName, string itemName, bool overwriteExistingItems)
        {
            CloneTestDataItem(false, categoryName, itemName, categoryName, itemName, overwriteExistingItems);
        }

        /// <summary>
        /// Copy all data from Global to local repository indicting if successful or not.
        /// </summary>
        /// <param name="overwriteExistingItems">Indicates if existing data should be overwritten.  An exception is thrown if false and any existing item/s would be overwritten.</param>
        /// <returns>True if successful clone or false if not (See <see cref="RepositoryLastTryException"/> for exception thrown if  false returned).</returns>
        /// <remarks>A deep clone is performed to copy items.  However, it should be noted that complex data items (EG. IO refeferences etc) may clone incorrectly.  Complex type cloning should be verified during test framework development to ensure correct cloning.</remarks>
        public static bool TryCloneGlobalToLocal(bool overwriteExistingItems)
        {
            try
            {
                CloneTestData(false, overwriteExistingItems);
                return true;
            }
            catch (Exception ex)
            {
                lastException[Thread.CurrentThread.ManagedThreadId] = ex;
                return false;
            }
        }

        /// <summary>
        /// Copy all data from required Global category to local repository indicting if successful or not.
        /// </summary>
        /// <param name="categoryName">Name of category to be copied.</param>
        /// <param name="overwriteExistingItems">Indicates if existing data should be overwritten.  An exception is thrown if false and any existing item/s would be overwritten.</param>
        /// <returns>True if successful clone or false if not (See <see cref="RepositoryLastTryException"/> for exception thrown if  false returned).</returns>
        /// <remarks>A deep clone is performed to copy items.  However, it should be noted that complex data items (EG. IO refeferences etc) may clone incorrectly.  Complex type cloning should be verified during test framework development to ensure correct cloning.</remarks>
        public static bool TryCloneCategoryGlobalToLocal(string categoryName, bool overwriteExistingItems)
        {
            try
            {
                CloneTestDataCategory(false, categoryName, categoryName, overwriteExistingItems);
                return true;
            }
            catch (Exception ex)
            {
                lastException[Thread.CurrentThread.ManagedThreadId] = ex;
                return false;
            }
        }

        /// <summary>
        /// Copy named data item from required Global category to Local repository indicating if successful
        /// </summary>
        /// <param name="categoryName">Name of category to be copied.</param>
        /// <param name="itemName">Name of item to clone.</param>
        /// <param name="overwriteExistingItems">Indicates if existing data item should be overwritten if it already exists.  An exception is thrown if false and the named data item exists in global repository.</param>
        /// <returns>True if successful clone or false if not (See <see cref="RepositoryLastTryException"/> for exception thrown if  false returned).</returns>
        /// <remarks>A deep clone is performed to copy items.  However, it should be noted that complex data items (EG. IO refeferences etc) may clone incorrectly.  Complex type cloning should be verified during test framework development to ensure correct cloning.</remarks>
        public static bool TryCloneItemGlobalToLocal(string categoryName, string itemName, bool overwriteExistingItems)
        {
            try
            {
                CloneTestDataItem(false, categoryName, itemName, categoryName, itemName, overwriteExistingItems);
                return true;
            }
            catch (Exception ex)
            {
                lastException[Thread.CurrentThread.ManagedThreadId] = ex;
                return false;
            }
        }

        /// <summary>
        /// Clear data from Global and all thread repositories
        /// </summary>
        /// <remarks>Should be used carefully as ALL data from Global and local repositories is removed.  Many TeamControlium libraries use Repository data to hold configuration settings and defaults.  If used after clearing repository these will not be available and errors may be thrown.</remarks>
        public static void ClearRepositoryAll()
        {
            lock (repository)
            {
                WriteLogLine(LogLevels.FrameworkDebug, $"Clearing all Test Data repositories (Global and all threads!)");
                repository.Clear();
            }
        }

        /// <summary>
        /// Clear data from Local repository
        /// </summary>
        /// <remarks>Should be used carefully as ALL data from the local repository is removed.  Many TeamControlium libraries use the local thread's repository to hold configuration data.  If this is cleared it should be rebuilt if required to ensure errors are not thrown.</remarks>
        public static void ClearRepositoryLocal()
        {
            lock (repository)
            {
                clearRepository(true);
            }
        }

        /// <summary>
        /// Clear data from Global repository
        /// </summary>
        public static void ClearRepositoryGlobal()
        {
            lock (repository)
            {
                clearRepository(false);
            }
        }


        /// <summary>
        /// Called to store exception information when a Try... method catches an Exception.  Allows actual exception type to be stored
        /// so that reader can determine underlying exception type.
        /// </summary>
        /// <typeparam name="T">Exception derived from Exception class.</typeparam>
        /// <param name="ex">Exception to be stored</param>
        private static void SetLastException<T>(T ex) where T : Exception
        {
            int threadID = Thread.CurrentThread.ManagedThreadId;
            lock (lastException)
            {
                if (lastException.ContainsKey(threadID))
                {
                    lastException[threadID] = ex;
                }
                else
                {
                    lastException.Add(threadID, ex);
                }
            }
        }


        /// <summary>
        /// Retrieves Dictionary, containing all data items for the named category, from the Local or Global repository
        /// </summary>
        /// <param name="isLocal">Indicates if local repository should be used (true) or global (false)</param>
        /// <param name="category">Name of category to obtain</param>
        /// <returns>Dictionary of data items in required repository's named category</returns>
        /// <remarks>Method is NOT thread safe.  It is the responsibility of the calling code to ensure thread safety.
        /// If category does not exist or there is another issue retrieving the category an exception will be thrown.</remarks>
        private static Dictionary<string, dynamic> getCategory(bool isLocal, string category)
        {
            // Get thread ID of current thread if local.  Or fixed non-thread ID if Global
            int threadID = isLocal ? Thread.CurrentThread.ManagedThreadId : globalIndex;

            // If passed category name is not populated throw an error indicating it must be populated.
            if (string.IsNullOrEmpty(category))
            {
                throw new ArgumentException(string.Format("Cannot be null or empty ({0})", category == null ? "Is Null" : "Is empty"), "Category");
            }

            // If the central repository does not contain data for the required thread (or Global) id throw an error.
            if (!repository.ContainsKey(threadID))
            {
                if (threadID == globalIndex)
                {
                    throw new Exception("Global Repository has no data!");
                }
                else
                {
                    throw new Exception($"Thread [{threadID}] Repository has no data!");
                }
            }

            // If the required (local or global) does not contain the required category, throw an error.
            if (!repository[threadID].ContainsKey(category))
            {
                if (threadID == globalIndex)
                {
                    throw new Exception($"Global repository does not have a category [{category}]");
                }
                else
                {
                    throw new Exception($"Thread [{threadID}] repository does not have a category [{category}]");
                }
            }

            // Return the required category from the required repository
            return repository[threadID][category];
        }

        /// <summary>
        /// Add the passed category dictionary to the required local/global repository.
        /// </summary>
        /// <param name="isLocal">Indicates if local repository should be used (true) or global (false)</param>
        /// <param name="name">Name of category being added</param>
        /// <param name="categoryToAdd">Dictionary to add</param>
        /// <param name="overwriteDuplicates">Flag stating if existing data items can be overwritten.</param>
        /// <remarks>If the named category <paramref name="overwriteDuplicates"/> is false and the repository category already exists with </remarks>
        private static void setCategory(bool isLocal, string name, Dictionary<string, dynamic> categoryToAdd, bool overwriteDuplicates)
        {
            int threadID = isLocal ? Thread.CurrentThread.ManagedThreadId : globalIndex;

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(string.Format("Cannot be null or empty ({0})", name == null ? "Is Null" : "Is empty"), "name");
            }

            verifyThreadIDExists(isLocal, true);

            if (!verifyCategoryIsNotNullOrEmptyAndExists(isLocal, name, false))
            {
                repository[threadID].Add(name, categoryToAdd);
            }
            else
            {
                if (overwriteDuplicates)
                {
                    categoryToAdd.ToList().ForEach(itemToAdd => repository[threadID][name][itemToAdd.Key] = itemToAdd.Value);
                }
                else
                {
                    try
                    {
                        categoryToAdd.ToList().ForEach(itemToAdd =>
                        {
                            if (repository[threadID][name].ContainsKey(itemToAdd.Key))
                            {
                                throw new ArgumentException($"Item [{itemToAdd.Key}] already exists");
                            }
                            repository[threadID][name].Add(itemToAdd.Key, itemToAdd.Value);
                        });
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Cannot merge category [{name}] into {(isLocal ? "Local" : "Global")} repository!", ex);
                    }
                }
                repository[threadID][name] = categoryToAdd;
            }

        }

        private static dynamic getItem(bool isLocal, string category, string itemKey)
        {
            verifyItemNameIsNotNullOrEmptyAndExists(isLocal, category, itemKey, true);

            // Get item named from categoryName and return it
            dynamic obtainedObject = (getCategory(isLocal, category))[itemKey];

            WriteLog(LogLevels.FrameworkDebug, "Got ");
            if (obtainedObject is string)
            {
                WriteLog(LogLevels.FrameworkDebug, "string [{0}]", ((string)obtainedObject)?.Length < 50 ? (string)obtainedObject : (((string)obtainedObject).Substring(0, 47) + "...") ?? "");
            }
            else if (obtainedObject is int)
            {
                WriteLog(LogLevels.FrameworkDebug, "integer [{0}]", ((int)obtainedObject));
            }
            else if (obtainedObject is float)
            {
                WriteLog(LogLevels.FrameworkDebug, "integer [{0}]", ((float)obtainedObject));
            }
            else
            {
                WriteLog(LogLevels.FrameworkDebug, "type {0}{1}", obtainedObject.ToString(), (obtainedObject == null) ? " (is null!)" : "");
            }
            WriteLogLine(LogLevels.FrameworkDebug, " from [{0}][{1}]", category, itemKey);
            return obtainedObject;
        }

        private static void setItem(bool isLocal, string category, string itemKey, dynamic value)
        {
            int threadID = isLocal ? Thread.CurrentThread.ManagedThreadId : globalIndex;

            WriteLog(LogLevels.FrameworkDebug, $"Setting [{(isLocal ? "Global" : $"Local(ThreadID { threadID })")}][{category}][{itemKey}] to ");
            if (value is string)
            {
                WriteLog(LogLevels.FrameworkDebug, "string [{0}]", ((string)value)?.Length < 50 ? (string)value : (((string)value).Substring(0, 47) + "...") ?? "");
            }
            else if (value is int)
            {
                WriteLog(LogLevels.FrameworkDebug, "integer [{0}]", ((int)value));
            }
            else if (value is float)
            {
                WriteLog(LogLevels.FrameworkDebug, "float [{0}]", ((float)value));
            }
            else
            {
                WriteLog(LogLevels.FrameworkDebug, "type {0}{1}", value.ToString(), (value == null) ? " (is null!)" : "");
            }

            //
            // Do we have the ThreadID?
            //
            if (!verifyThreadIDExists(isLocal, false))
                repository.Add(threadID, new Dictionary<string, Dictionary<string, dynamic>>());
            // Add Category if we dont already have it
            if (!verifyCategoryIsNotNullOrEmptyAndExists(isLocal, category, false))
                repository[threadID].Add(category, new Dictionary<string, dynamic>());

            Dictionary<string, dynamic> wholeCategory = repository[threadID][category];
            // Add Name if we dont already have it in the current categoryName, otherwise change contents of name
            if (wholeCategory.ContainsKey(itemKey))
                wholeCategory[itemKey] = value;
            else
                wholeCategory.Add(itemKey, value);
        }

        private static U getItemTyped<U>(bool isLocal, string category, string item)
        {
            dynamic obtainedObject;

            try
            {
                obtainedObject = getItem(isLocal, category, item);

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
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Exception getting Category.Name ([{0}].[{1}])", category, item), ex);
            }

            throw new Exception(string.Format("Expected type [{0}] but got type [{1}].", typeof(U).Name, obtainedObject.GetType()));

        }

        private static void clearRepository(bool isLocal)
        {
            var threadID = isLocal ? Thread.CurrentThread.ManagedThreadId : globalIndex;
            if (repository.ContainsKey(threadID))
            {
                if (isLocal)
                {
                    WriteLogLine(LogLevels.FrameworkDebug, $"Clearing repository for threadID {threadID}");
                }
                else
                {
                    WriteLogLine(LogLevels.FrameworkDebug, $"Clearing global repository");
                }
                repository[threadID].Clear();
            }
        }

        private static void CloneTestData(bool fromLocal,bool overwriteIfExists)
        {
            int fromThreadID = fromLocal ? Thread.CurrentThread.ManagedThreadId : globalIndex;
            int toThreadID = !fromLocal ? Thread.CurrentThread.ManagedThreadId : globalIndex;

            verifyThreadIDExists(fromLocal, true);

            foreach(var categoryName in repository[fromThreadID].Keys)
            {
                CloneTestDataCategory(fromLocal, categoryName, categoryName, overwriteIfExists);
            }
            
        }

        private static void CloneTestDataCategory(bool fromLocal, string fromCategoryName, string toCategoryName, bool overwriteIfExists)
        {
            lock (repository)
            {
                int toThreadID = !fromLocal ? Thread.CurrentThread.ManagedThreadId : globalIndex;

                verifyCategoryIsNotNullOrEmptyAndExists(fromLocal, fromCategoryName, true);

                foreach (var item in getCategory(fromLocal, fromCategoryName))
                {
                    CloneTestDataItem(fromLocal, fromCategoryName, item.Key, toCategoryName, item.Key, overwriteIfExists);
                }
            }
        }
        private static void CloneTestDataItem(bool fromLocal, string fromCategoryName, string fromItemName, string toCategoryName, string toItemName, bool overwriteIfExists)
        {
            lock (repository)
            {
                int toThreadID = !fromLocal ? Thread.CurrentThread.ManagedThreadId : globalIndex;

                if (!verifyThreadIDExists(!fromLocal, false))
                {
                    repository.Add(toThreadID, new Dictionary<string, Dictionary<string, dynamic>>());
                }
                if (!verifyCategoryIsNotNullOrEmptyAndExists(!fromLocal, toCategoryName, false))
                {
                    repository[toThreadID].Add(toCategoryName, new Dictionary<string, dynamic>());
                }



                if (string.IsNullOrEmpty(toItemName))
                {
                    throw new Exception("'To' Item name must not be NULL or empty!");
                }

                dynamic fromItem = getItem(fromLocal, fromCategoryName, fromItemName);

                switch (isTypeCloneable(fromItem.GetType()))
                {
                    case CloneableTypes.yes:
                        {
                            if (getCategory(!fromLocal, toCategoryName).ContainsKey(toItemName) && !overwriteIfExists)
                            {
                                throw new Exception($"'To' item [{toItemName}] in {(!fromLocal ? $"Local (ThreadID { toThreadID })" : "Global")} repository's [{toCategoryName}] category already exists and overwriteIfExists is false!");
                            }
                            else
                            {
                                setItem(!fromLocal, toCategoryName, toItemName, fromItem.Clone());
                            }

                        }
                        break;
                    case CloneableTypes.no:
                    case CloneableTypes.isValueType:
                        {
                            if (getCategory(!fromLocal, toCategoryName).ContainsKey(toItemName))
                            {
                                if (overwriteIfExists)
                                {
                                    getCategory(!fromLocal, toCategoryName)[toItemName] = fromItem.Value;
                                }
                                else
                                {
                                    throw new Exception($"'To' item [{toItemName}] in {(toThreadID == globalIndex ? "Global" : $"Local (ThreadID { toThreadID })")} repository's [{toCategoryName}] category already exists and overwriteIfExists is false!");
                                }
                            }
                            else
                            {
                                getCategory(!fromLocal, toCategoryName).Add(toItemName, fromItem.Value);
                            }

                            if (isTypeCloneable(fromItem.Value.GetType()) == CloneableTypes.no)
                            {
                                Log.WriteLogLine(LogLevels.TestInformation, $"Cannot clone [{fromCategoryName}][{fromItemName}] (Type: {fromItem.Value.GetType().ToString()}) from {(toThreadID == globalIndex ? "Global" : $"Local (ThreadID { toThreadID })")} repository to {(toThreadID == globalIndex ? "Global" : $"Local (ThreadID { toThreadID })")}. Copying reference instead.");
                            }
                        }
                        break;
                    default:
                        {
                            if (getCategory(!fromLocal, toCategoryName).ContainsKey(toItemName))
                            {
                                if (overwriteIfExists)
                                {
                                    getCategory(!fromLocal, toCategoryName)[toItemName] = fromItem.Value;
                                }
                                else
                                {
                                    throw new Exception($"'To' item [{toItemName}] in {(toThreadID == globalIndex ? "Global" : $"Local (ThreadID { toThreadID })")} repository's [{toCategoryName}] category already exists and overwriteIfExists is false!");
                                }
                            }
                            else
                            {
                                getCategory(!fromLocal, toCategoryName).Add(toItemName, fromItem.Value);
                            }
                        }
                        break;
                }
            }
        }

        private enum CloneableTypes { isValueType, yes, no }

        private static CloneableTypes isTypeCloneable(Type type)
        {
            if (typeof(ICloneable).IsAssignableFrom(type))
            {
                return CloneableTypes.yes;
            }
            else if (type.IsValueType)
            {
                return CloneableTypes.isValueType;
            }
            else
            {
                return CloneableTypes.no;
            }
        }

        private static bool verifyThreadIDExists(bool isLocal, bool throwException)
        {
            int threadID = isLocal ? Thread.CurrentThread.ManagedThreadId : globalIndex;
            if (!repository.ContainsKey(threadID))
            {
                if (throwException)
                {
                    throw new Exception($"No repository for {(isLocal ? $"Local ({threadID})" : "Global")}.  ThreadID must reference an existing repository");
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        private static bool verifyCategoryIsNotNullOrEmptyAndExists(bool isLocal, string categoryName, bool throwException)
        {
            int threadID = isLocal ? Thread.CurrentThread.ManagedThreadId : globalIndex;

            if (!verifyThreadIDExists(isLocal, throwException))
            {
                return false;
            }

            if (string.IsNullOrEmpty(categoryName))
            {
                if (throwException)
                {
                    throw new Exception($"Category name is {(categoryName == null ? "NULL" : "Empty")}. Category name must be valid and exist");
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (!repository[threadID].ContainsKey(categoryName))
                {
                    if (categoryName==categorylessItems)
                    {
                        repository[threadID].Add(categorylessItems, new Dictionary<string, dynamic>());
                    }
                    if (throwException)
                    {
                        throw new Exception($"Category name {categoryName} does not exist in {(isLocal ? $"Local (ThreadID {threadID})" : "Global")} repository. Category name must be valid and exist");
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
        }

        private static bool verifyItemNameIsNotNullOrEmptyAndExists(bool isLocal, string categoryName, string itemName, bool throwException)
        {
            if (!verifyCategoryIsNotNullOrEmptyAndExists(isLocal, categoryName, throwException))
            {
                return false;
            }

            if (string.IsNullOrEmpty(categoryName))
            {
                if (throwException)
                {
                    throw new Exception($"Category name is {(categoryName == null ? "NULL" : "Empty")}. Category name must be valid and exist");
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (!getCategory(isLocal, categoryName).ContainsKey(itemName))
                {
                    if (throwException)
                    {
                        throw new Exception($"Item {itemName} does not exist in {(isLocal ? $"Local (ThreadID {Thread.CurrentThread.ManagedThreadId})" : "Global")} repository's [{categoryName}] category. Item name must be valid and exist");
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
        }

        public class DynamicItems<dynamic>
        {
            private bool isLocal;

            public DynamicItems(bool isLocal)
            {
                this.isLocal = isLocal;

            }
            public dynamic this[string item]
            {
                get
                {
                    lock (repository)
                    {
                        return this[categorylessItems, item];
                    }
                }

                set
                {
                    lock (repository)
                    {
                        this[categorylessItems, item] = value;
                    }
                }
            }
            public dynamic this[string category, string item]
            {
                get
                {
                    lock (repository)
                    {
                        return getItem(isLocal, category, item);
                    }
                }

                set
                {
                    lock (repository)
                    {
                        setItem(isLocal, category, item, value);
                    }
                }

            }
        }
    }
}
