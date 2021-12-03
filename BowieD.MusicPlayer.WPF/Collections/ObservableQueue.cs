using System.Collections.ObjectModel;

namespace BowieD.MusicPlayer.WPF.Collections
{
    public class ObservableQueue<T> : ObservableCollection<T>
    {
        public void Enqueue(T value)
        {
            Add(value);
        }

        public T Dequeue()
        {
            var item = this[0];

            RemoveAt(0);

            return item;
        }

        public T Peek()
        {
            return this[0];
        }
    }
}
