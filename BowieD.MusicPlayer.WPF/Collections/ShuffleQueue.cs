using BowieD.MusicPlayer.WPF.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace BowieD.MusicPlayer.WPF.Collections
{
    /// <summary>
    /// Keeps two instances of queue, one being not shuffled, and one shuffled
    /// </summary>
    public class ShuffleQueue<T> : ObservableLIFOFIFO<T>
    {
        private bool _isShuffled = false;
        private readonly List<T> _notShuffledState = new();

        public ShuffleQueue()
        {
            CollectionChanged += ShuffleQueue_CollectionChanged;
        }

        private void ShuffleQueue_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_pauseEvent)
                return;

            if (IsShuffled)
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        {
                            if (e.NewItems is not null)
                            {
                                _notShuffledState.AddRange(e.NewItems.Cast<T>());
                            }
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        {
                            if (e.OldItems is not null)
                            {
                                foreach (var t in e.OldItems)
                                {
                                    if (t is T item)
                                    {
                                        _notShuffledState.Remove(item);
                                    }
                                }
                            }
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                        {
                            if (e.OldItems is not null)
                            {
                                foreach (var t in e.OldItems)
                                {
                                    if (t is T item)
                                    {
                                        _notShuffledState.Remove(item);
                                    }
                                }
                            }

                            if (e.NewItems is not null)
                            {
                                foreach (var t in e.NewItems)
                                {
                                    if (t is T item)
                                    {
                                        _notShuffledState.Add(item);
                                    }
                                }
                            }
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        {
                            _notShuffledState.Clear();
                            _notShuffledState.AddRange(this);
                        }
                        break;
                }
            }
        }

        private bool _pauseEvent = false;

        public bool IsShuffled
        {
            get => _isShuffled;
            set
            {
                if (value != _isShuffled)
                {
                    _isShuffled = value;

                    _pauseEvent = true;

                    if (value) // shuffle it
                    {
                        _notShuffledState.Clear();

                        _notShuffledState.AddRange(this);

                        this.Clear();

                        foreach (var s in _notShuffledState.ShuffleLinq())
                        {
                            this.Add(s);
                        }
                    }
                    else // unshuffle it
                    {
                        this.Clear();

                        foreach (var nss in _notShuffledState)
                        {
                            this.Add(nss);
                        }
                    }

                    _pauseEvent = false;
                }
            }
        }

        public IEnumerable<T> GetUnshuffled()
        {
            if (IsShuffled)
                return _notShuffledState;

            return this;
        }
    }
}
