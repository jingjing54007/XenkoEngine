﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Presentation.Core;

namespace SiliconStudio.Presentation.Behaviors
{
    public class ItemsControlCollectionViewBehavior : Behavior<ItemsControl>
    {
        private readonly DependencyPropertyWatcher propertyWatcher = new DependencyPropertyWatcher();

        public static readonly DependencyProperty GroupingPropertyNameProperty = DependencyProperty.Register("GroupingPropertyName", typeof(string), typeof(ItemsControlCollectionViewBehavior), new PropertyMetadata(null, GroupingPropertyNameChanged));

        public static readonly DependencyProperty FilterPredicateProperty = DependencyProperty.Register("FilterPredicate", typeof(Predicate<object>), typeof(ItemsControlCollectionViewBehavior), new PropertyMetadata(null, FilterPredicateChanged));

        public string GroupingPropertyName { get { return (string)GetValue(GroupingPropertyNameProperty); } set { SetValue(GroupingPropertyNameProperty, value); } }
  
        public Predicate<object> FilterPredicate { get { return (Predicate<object>)GetValue(FilterPredicateProperty); } set { SetValue(FilterPredicateProperty, value); } }

        public IValueConverter GroupingPropertyConverter { get; set; }

        protected override void OnAttached()
        {
            base.OnAttached();
            propertyWatcher.Attach(AssociatedObject);
            propertyWatcher.RegisterValueChangedHandler(ItemsControl.ItemsSourceProperty, ItemsSourceChanged);
            UpdateCollectionView();
        }

        protected override void OnDetaching()
        {
            propertyWatcher.Detach();
            base.OnDetaching();
        }

        private void UpdateCollectionView()
        {
            if (AssociatedObject?.ItemsSource != null)
            {
                var collectionView = (CollectionView)CollectionViewSource.GetDefaultView(AssociatedObject.ItemsSource);
                if (collectionView == null) throw new InvalidOperationException("CollectionViewSource.GetDefaultView returned null for the items source of the associated object.");
                using (collectionView.DeferRefresh())
                {
                    bool removeGrouping = string.IsNullOrWhiteSpace(GroupingPropertyName);
                    if (collectionView.CanGroup)
                    {
                        if (collectionView.GroupDescriptions == null) throw new InvalidOperationException("CollectionView does not have a group description collection.");
                        collectionView.GroupDescriptions.Clear();

                        if (!removeGrouping)
                        {
                            var groupDescription = new PropertyGroupDescription(GroupingPropertyName, GroupingPropertyConverter);
                            collectionView.GroupDescriptions.Add(groupDescription);
                        }
                    }
                    if (collectionView.CanFilter)
                    {
                        collectionView.Filter = FilterPredicate;
                    }
                }
            }
        }

        private void ItemsSourceChanged(object sender, EventArgs e)
        {
            UpdateCollectionView();
        }

        private static void GroupingPropertyNameChanged([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (ItemsControlCollectionViewBehavior)d;
            behavior.UpdateCollectionView();
        }

        private static void FilterPredicateChanged([NotNull] DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (ItemsControlCollectionViewBehavior)d;
            behavior.UpdateCollectionView();
        }
    }
}

