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
    /// 
    /// </summary>
    public class Repository
    {
        /// <summary>
        /// Actual storage of data.  Three dimensional dictionary.
        /// 1st dimension is the ThreadID storing data for the identified specific thread.
        /// 2nd dimension is the data category
        /// 3rd dimension is the data item name
        /// Data is stored as dynamic objects enabling storage of anything and runtime changes of item location types as well as varying types in the repository.
        /// </summary>
        private static Dictionary<int, Dictionary<string, Dictionary<string, dynamic>>> repository = new Dictionary<int, Dictionary<string, Dictionary<string, dynamic>>>();

        /// <summary>
        /// When data is stored in the Global repository (IE. Seen by all threads) the threadID used is -1.
        /// </summary>
        private static int globalIndex = -1; // Index of Global TestData (IE. Test Data available to all threads)

        private static string noCategoryName = "NoCategoryName";

        private static Dictionary<int, Exception> lastException = new Dictionary<int, Exception>();


        private static void SetLastException(Exception ex)
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
        /// Returns the last exception were a TryGetRunCategoryOptions returned false
        /// </summary>
        public static Exception RepositoryLastTryException()
        {
            int threadID = Thread.CurrentThread.ManagedThreadId;
            if (lastException.ContainsKey(threadID))
            {
                return lastException[threadID];
            }
            else
            {
                return null;
            }
        }

        public static dynamic ItemLocal { get; } = new DynamicItems<dynamic>(true);
        public static dynamic ItemGlobal { get; } = new DynamicItems<dynamic>(false);


        public static bool HasCategoryLocal(string category)
        {
            lock (repository)
            {
                return verifyCategoryIsNotNullOrEmptyAndExists(false, category, false);
            }
        }
        public static bool HasCategoryGlobal(string category)
        {
            return verifyCategoryIsNotNullOrEmptyAndExists(false, category, false);
        }
        public static Dictionary<string, dynamic> GetCategoryLocal(string categoryName)
        {
            return getCategory(true, categoryName);
        }
        public static Dictionary<string, dynamic> GetCategoryGlobal(string categoryName)
        {
            return getCategory(false, categoryName);
        }
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
        public static void SetCategoryLocal(string name, Dictionary<string, dynamic> category)
        {
            setCategory(true, name, category);
        }
        public static void SetCategoryGlobal(string name, Dictionary<string, dynamic> category)
        {
            setCategory(false, name, category);
        }



        public static dynamic GetItemLocal(string itemName)
        {
            lock (repository)
            {
                return getItem(true, noCategoryName, itemName);
            }
        }

        public static dynamic GetItemGlobal(string itemName)
        {
            lock (repository)
            {
                return getItem(false, noCategoryName, itemName);
            }
        }

        public static dynamic GetItemLocal(string categoryName, string itemName)
        {
            lock (repository)
            {
                return getItem(true, categoryName, itemName);
            }
        }

        public static dynamic GetItemGlobal(string categoryName, string itemName)
        {
            lock (repository)
            {
                return getItem(false, categoryName, itemName);
            }
        }

        public static T GetItemLocal<T>(string itemName)
        {
            lock (repository)
            {
                return getItemTyped<T>(true, noCategoryName, itemName);
            }
        }

        public static T GetItemGlobal<T>(string itemName)
        {
            lock (repository)
            {
                return getItemTyped<T>(false, noCategoryName, itemName);
            }
        }

        public static T GetItemLocal<T>(string categoryName, string itemName)
        {
            lock (repository)
            {
                return getItemTyped<T>(true, categoryName, itemName);
            }
        }

        public static T GetItemGlobal<T>(string categoryName, string itemName)
        {
            lock (repository)
            {
                return getItemTyped<T>(false, categoryName, itemName);
            }
        }

        public static bool TryGetItemLocal(string itemName, out dynamic item)
        {
            lock (repository)
            {
                try
                {
                    item = getItem(true, noCategoryName, itemName);
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

        public static bool TryGetItemGlobal(string itemName, out dynamic item)
        {
            lock (repository)
            {
                try
                {
                    item = getItem(false, noCategoryName, itemName);
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

        public static bool TryGetItemLocal<T>(string itemName, out T item)
        {
            lock (repository)
            {
                try
                {
                    item = getItemTyped<T>(true, noCategoryName, itemName);
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

        public static bool TryGetItemGlobal<T>(string itemName, out T item)
        {
            lock (repository)
            {
                try
                {
                    item = getItemTyped<T>(false, noCategoryName, itemName);
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

        public static T GetItemOrDefaultLocal<T>(string itemName)
        {
            lock (repository)
            {
                try
                {
                    lock (repository)
                    {
                        return getItemTyped<T>(true, noCategoryName, itemName);
                    }
                }
                catch
                {
                    return default(T);
                }
            }
        }

        public static T GetItemOrDefaultGlobal<T>(string itemName)
        {
            lock (repository)
            {
                try
                {
                    lock (repository)
                    {
                        return getItemTyped<T>(false, noCategoryName, itemName);
                    }
                }
                catch
                {
                    return default(T);
                }
            }
        }

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

        public static void SetItemLocal(string itemName,dynamic item)
        {
            setItem(true, noCategoryName, itemName, item);
        }
        public static void SetItemGlobal(string itemName, dynamic item)
        {
            setItem(false, noCategoryName, itemName, item);
        }

        public static void SetItemLocal(string categoryName, string itemName, dynamic item)
        {
            setItem(true, categoryName, itemName, item);
        }
        public static void SetItemGlobal(string categoryName, string itemName, dynamic item)
        {
            setItem(false, categoryName, itemName, item);
        }

        public static void CloneLocalToGlobal(bool overwriteExistingItems)
        {
            CloneTestData(true, overwriteExistingItems);
        }

        public static void CloneCategoryLocalToGlobal(string categoryName,bool overwriteExistingItems)
        {
            CloneTestDataCategory(true, categoryName, categoryName, overwriteExistingItems);
        }

        public static void CloneItemLocalToGlobal(string categoryName, string itemName, bool overwriteExistingItems)
        {
            CloneTestDataItem(true, categoryName, itemName, categoryName, itemName, overwriteExistingItems);
        }

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


        public static void CloneGlobalToLocal(bool overwriteExistingItems)
        {
            CloneTestData(false, overwriteExistingItems);
        }

        public static void CloneCategoryGlobalToLocal(string categoryName, bool overwriteExistingItems)
        {
            CloneTestDataCategory(false, categoryName, categoryName, overwriteExistingItems);
        }

        public static void CloneItemGlobalToLocal(string categoryName, string itemName, bool overwriteExistingItems)
        {
            CloneTestDataItem(false, categoryName, itemName, categoryName, itemName, overwriteExistingItems);
        }

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




        public static void ClearRepositoryAll()
        {
            WriteLogLine(LogLevels.FrameworkDebug, $"Clearing all Test Data repositories (Global and all threads!)");
            repository.Clear();
        }

        public static void ClearRepositoryLocal()
        {
            lock (repository)
            {
                clearRepository(true);
            }
        }

        public static void ClearRepositoryGlobal()
        {
            lock (repository)
            {
                clearRepository(false);
            }
        }


        private static Dictionary<string, dynamic> getCategory(bool isLocal, string category)
        {
            int threadID = isLocal ? Thread.CurrentThread.ManagedThreadId : globalIndex;

            if (string.IsNullOrEmpty(category))
            {
                throw new ArgumentException(string.Format("Cannot be null or empty ({0})", category == null ? "Is Null" : "Is empty"), "Category");
            }


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

            return repository[threadID][category];
        }

        private static void setCategory(bool isLocal, string name, Dictionary<string, dynamic> category)
        {
            int threadID = isLocal ? Thread.CurrentThread.ManagedThreadId : globalIndex;

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(string.Format("Cannot be null or empty ({0})", name == null ? "Is Null" : "Is empty"), "name");
            }

            verifyThreadIDExists(isLocal, true);

            if (!verifyCategoryIsNotNullOrEmptyAndExists(isLocal, name, false))
            {
                repository[threadID].Add(name, category);
            }
            else
            {
                repository[threadID][name] = category;
            }

        }

        private static dynamic getItem(bool isLocal, string category, string itemKey)
        {
            verifyItemNameIsNotNullOrEmptyAndExists(isLocal, category, itemKey, true);

            // Get item named from category and return it
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
            // Add Name if we dont already have it in the current category, otherwise change contents of name
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
            int toThreadID = !fromLocal ? Thread.CurrentThread.ManagedThreadId : globalIndex;

            verifyCategoryIsNotNullOrEmptyAndExists(fromLocal, fromCategoryName, true);

            foreach (var item in getCategory(fromLocal, fromCategoryName))
            {
                CloneTestDataItem(fromLocal, fromCategoryName, item.Key, toCategoryName, item.Key, overwriteIfExists);
            }
        }
        private static void CloneTestDataItem(bool fromLocal, string fromCategoryName, string fromItemName, string toCategoryName, string toItemName, bool overwriteIfExists)
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
                    if (categoryName==noCategoryName)
                    {
                        repository[threadID].Add(noCategoryName, new Dictionary<string, dynamic>());
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
                        return this[noCategoryName, item];
                    }
                }

                set
                {
                    lock (repository)
                    {
                        this[noCategoryName, item] = value;
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
