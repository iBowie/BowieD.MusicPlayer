using System.Collections.ObjectModel;

namespace BowieD.MusicPlayer.WPF.Collections
{
    public class ObservableStack<T> : ObservableCollection<T>
    {
        public virtual void Push(T item)
        {
            Add(item);
        }

        public T Peek()
        {
            return this[Count - 1];
        }

        public virtual T Pop()
        {
            var item = this[Count - 1];

            RemoveAt(Count - 1);

            return item;
        }
    }
}
