using System.Collections.ObjectModel;

namespace BowieD.MusicPlayer.WPF.Collections
{
    public class ObservableLIFOFIFO<T> : ObservableCollection<T>
    {
        /// <summary>
        /// LIFO - Stack
        /// </summary>
        public virtual void PushLIFO(T item)
        {
            Insert(0, item);
        }

        /// <summary>
        /// LIFO - Stack
        /// </summary>
        public T PeekLIFO()
        {
            return this[Count - 1];
        }

        /// <summary>
        /// LIFO - Stack
        /// </summary>
        public T PopLIFO()
        {
            var item = this[Count - 1];

            RemoveAt(Count - 1);

            return item;
        }

        /// <summary>
        /// FIFO - Queue
        /// </summary>
        public virtual void EnqueueFIFO(T item)
        {
            Add(item);
        }

        /// <summary>
        /// FIFO - Queue
        /// </summary>
        public T PeekFIFO()
        {
            return this[0];
        }

        /// <summary>
        /// FIFO - Queue
        /// </summary>
        public T DequeueFIFO()
        {
            var item = this[0];

            RemoveAt(0);

            return item;
        }
    }
}
