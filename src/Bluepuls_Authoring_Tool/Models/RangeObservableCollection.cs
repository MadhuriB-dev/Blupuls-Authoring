using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Bluepuls_Authoring_Tool.Models
{
    /// <summary>
    /// High-performance ObservableCollection supporting batch ReplaceAll and AddRange
    /// that fire a single Reset notification instead of per-item UI layout passes.
    /// </summary>
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification;

        public void ReplaceAll(IEnumerable<T> items)
        {
            CheckReentrancy();
            _suppressNotification = true;
            try
            {
                Items.Clear();
                if (items != null)
                {
                    foreach (var item in items)
                        Items.Add(item);
                }
            }
            finally
            {
                _suppressNotification = false;
            }

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) return;
            CheckReentrancy();
            _suppressNotification = true;
            try
            {
                foreach (var item in items)
                    Items.Add(item);
            }
            finally
            {
                _suppressNotification = false;
            }

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
                base.OnCollectionChanged(e);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!_suppressNotification)
                base.OnPropertyChanged(e);
        }
    }
}
