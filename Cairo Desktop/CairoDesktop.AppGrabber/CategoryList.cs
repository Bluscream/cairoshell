﻿using System;
using System.Collections.ObjectModel;
using System.Xml;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;

namespace CairoDesktop.AppGrabber {

    public class CategoryList : ObservableCollection<Category> {

        public const int MIN_CATEGORIES = 3;
        public event EventHandler<EventArgs> CategoryChanged;

        /// <summary>
        /// Simple wrapper around an ObservableCollection of Category objects.
        /// </summary>
        public CategoryList(bool firstStart = false) {
            // add default categories
            Add(new Category("All", true, AppCategoryType.All));

            if (firstStart)
            {
                Add(new Category("Uncategorized", false, AppCategoryType.Uncategorized));
                Add(new Category("Quick Launch", false, AppCategoryType.QuickLaunch));
            }
        }

        /// <summary>
        /// Returns the category with the specified name.
        /// </summary>
        public Category GetCategory(string categoryName) {
            foreach (Category c in this) {
                if (c.Name == categoryName) {
                    return c;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the category with the specified name.
        /// </summary>
        public Category GetSpecialCategory(AppCategoryType type)
        {
            foreach (Category c in this)
            {
                if (c.Type == type)
                {
                    return c;
                }
            }

            // category wasn't found
            Category cat;
            switch (type)
            {
                case AppCategoryType.All:
                    cat = new Category("All", true, AppCategoryType.All);
                    Add(cat);
                    return cat;
                case AppCategoryType.Uncategorized:
                    cat = new Category("Uncategorized", true, AppCategoryType.Uncategorized);
                    Add(cat);
                    return cat;
                case AppCategoryType.QuickLaunch:
                    cat = new Category("Quick Launch", true, AppCategoryType.QuickLaunch);
                    Add(cat);
                    return cat;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Add a Category object to this CategoryList.
        /// </summary>
        /// <param name="category">Category to add.</param>
        public new void Add(Category category) {
            base.Add(category);
            category.ParentCategoryList = this;
            category.CollectionChanged += Category_CollectionChanged;
        }

        /// <summary>
        /// Removes a Category object from this CategoryList.
        /// </summary>
        /// <param name="category">Category to remove.</param>
        public new void Remove(Category category)
        {
            // Don't allow removal of special categories
            if (category.Type > 0) return;

            // Move apps to uncategorized
            Category uncategorized = GetSpecialCategory(AppCategoryType.Uncategorized);
            for (int i = category.Count - 1; i >= 0; i--)
            {
                ApplicationInfo app = category[i];
                category.RemoveAt(i);
                uncategorized.Add(app);
            }

            category.CollectionChanged -= Category_CollectionChanged;
            base.Remove(category);
        }

        private void Category_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnCategoryChanged(sender, new EventArgs());
        }

        /// <summary>
        /// Changes the index of the specified Category by the specified amount.
        /// Returns false if the index is outside the bounds of the list. Otherwise, true.
        /// </summary>
        /// <param name="category">Category to move.</param>
        /// <param name="delta">Number of places to move relative to starting index.</param>
        public bool MoveCategory(Category category, int delta) {
            int currentIndex = IndexOf(category);
            int requestedIndex = currentIndex + delta;
            if (requestedIndex < MIN_CATEGORIES || requestedIndex > Count - 1) {
                return false;
            } else {
                Move(currentIndex, requestedIndex);
            }
            return true;
        }

        public ObservableCollection<ApplicationInfo> FlatList {
            get {
                ObservableCollection<ApplicationInfo> rval = new ObservableCollection<ApplicationInfo>();
                foreach (Category cat in this) {
                    if (cat.Type == AppCategoryType.QuickLaunch || cat.Type == AppCategoryType.All) continue;
                    foreach (ApplicationInfo app in cat) {
                        rval.Add(app);
                    }
                }
                return rval;
            }
        }

        public void Serialize(string ConfigFile) {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("CategoryList");
            doc.AppendChild(root);
            foreach (Category cat in this) {
                if (cat.Type != AppCategoryType.All)
                {
                    XmlElement catElement = doc.CreateElement("Category");
                    XmlAttribute catNameAttribute = doc.CreateAttribute("Name");
                    catNameAttribute.Value = cat.Name;
                    catElement.Attributes.Append(catNameAttribute);

                    XmlAttribute catShowInMenuAttribute = doc.CreateAttribute("ShowInMenu");
                    catShowInMenuAttribute.Value = cat.ShowInMenu.ToString();
                    catElement.Attributes.Append(catShowInMenuAttribute);

                    XmlAttribute catTypeAttribute = doc.CreateAttribute("Type");
                    catTypeAttribute.Value = ((int)cat.Type).ToString();
                    catElement.Attributes.Append(catTypeAttribute);

                    root.AppendChild(catElement);
                    foreach (ApplicationInfo app in cat)
                    {
                        XmlElement appElement = doc.CreateElement("Application");
                        catElement.AppendChild(appElement);

                        XmlAttribute appAskAlwaysAdminAttribute = doc.CreateAttribute("AskAlwaysAdmin");
                        appAskAlwaysAdminAttribute.Value = app.AskAlwaysAdmin.ToString();
                        appElement.Attributes.Append(appAskAlwaysAdminAttribute);

                        XmlAttribute appAlwaysAdminAttribute = doc.CreateAttribute("AlwaysAdmin");
                        appAlwaysAdminAttribute.Value = app.AlwaysAdmin.ToString();
                        appElement.Attributes.Append(appAlwaysAdminAttribute);

                        XmlAttribute appAllowRunAsAdminAttribute = doc.CreateAttribute("AllowRunAsAdmin");
                        appAllowRunAsAdminAttribute.Value = app.AllowRunAsAdmin.ToString();
                        appElement.Attributes.Append(appAllowRunAsAdminAttribute);

                        XmlElement appNameElement = doc.CreateElement("Name");
                        appNameElement.InnerText = app.Name;
                        appElement.AppendChild(appNameElement);

                        XmlElement pathElement = doc.CreateElement("Path");
                        pathElement.InnerText = app.Path;
                        appElement.AppendChild(pathElement);

                        XmlElement targetElement = doc.CreateElement("Target");
                        targetElement.InnerText = app.Target;
                        appElement.AppendChild(targetElement);
                    }
                }
            }
            doc.Save(ConfigFile);
        }

        public static CategoryList Deserialize(string ConfigFile) {
            XmlDocument doc = new XmlDocument();
            doc.Load(ConfigFile);
            XmlElement root = doc.ChildNodes[0] as XmlElement;
            CategoryList catList = new CategoryList();
            foreach (XmlElement catElement in root.ChildNodes) {
                // get category
                Category cat = new Category();
                cat.Name = catElement.Attributes["Name"].Value;
                if (catElement.Attributes["Type"] != null)
                    cat.Type = (AppCategoryType)Convert.ToInt32(catElement.Attributes["Type"].Value);
                else
                {
                    // migration
                    if (cat.Name == "Uncategorized")
                        cat.Type = AppCategoryType.Uncategorized;
                    else if (cat.Name == "Quick Launch")
                        cat.Type = AppCategoryType.QuickLaunch;
                }
                if (catElement.Attributes["ShowInMenu"] != null)
                    cat.ShowInMenu = Convert.ToBoolean(catElement.Attributes["ShowInMenu"].Value);
                else
                {
                    // force hide quick launch and uncategorized
                    if (cat.Type == AppCategoryType.Uncategorized || cat.Type == AppCategoryType.QuickLaunch)
                        cat.ShowInMenu = false;
                }

                catList.Add(cat);

                foreach (XmlElement appElement in catElement.ChildNodes) {
                    // get application
                    ApplicationInfo app = new ApplicationInfo
                    {
                        Name = appElement.ChildNodes[0].InnerText,
                        Path = appElement.ChildNodes[1].InnerText
                    };

                    if (appElement.Attributes["AskAlwaysAdmin"] != null)
                        app.AskAlwaysAdmin = Convert.ToBoolean(appElement.Attributes["AskAlwaysAdmin"].Value);

                    if (appElement.Attributes["AlwaysAdmin"] != null)
                        app.AlwaysAdmin = Convert.ToBoolean(appElement.Attributes["AlwaysAdmin"].Value);

                    if (appElement.ChildNodes.Count > 2)
                        app.Target = appElement.ChildNodes[2].InnerText;

                    if (!app.IsStoreApp && !ShellHelper.Exists(app.Path) && (!app.Path.EndsWith(".lnk") || string.IsNullOrEmpty(app.Target) || !ShellHelper.Exists(app.Target)))
                    {
                        ShellLogger.Debug($"[AppGrabber] {app.Path} does not exist");
                        continue;
                    }

                    if (appElement.Attributes["AllowRunAsAdmin"] != null)
                        app.AllowRunAsAdmin = Convert.ToBoolean(appElement.Attributes["AllowRunAsAdmin"].Value);
                    else
                    {
                        // migration
                        if (app.IsStoreApp && EnvironmentHelper.IsWindows8OrBetter)
                        {
                            ManagedShell.UWPInterop.StoreApp storeApp = ManagedShell.UWPInterop.StoreAppHelper.AppList.GetAppByAumid(app.Target);

                            if (storeApp != null)
                            {
                                app.AllowRunAsAdmin = storeApp.EntryPoint == "Windows.FullTrustApplication";
                            }
                            else
                            {
                                app.AllowRunAsAdmin = false;
                            }
                        }
                        else
                        {
                            app.AllowRunAsAdmin = true;
                        }
                    }

                    cat.Add(app);
                }
            }
            return catList;
        }

        protected virtual void OnCategoryChanged(object sender, EventArgs e)
        {
            CategoryChanged?.Invoke(sender, e);
        }
    }
}
