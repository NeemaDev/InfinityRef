using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace InfinityRef.Core.Models
{
    public class Canvas
    {
        public Canvas()
        {
            Layers.CollectionChanged += OnLayersChanged;
        }

        public ObservableCollection<Layer> Layers { get; } = new ObservableCollection<Layer>();
        public int LayerCount => Layers.Count;

        public void AddLayer(Layer layer)
        {
            Layers.Add(layer);
        }

        public void RemoveLayer(Layer layer)
        {
            Layers.Remove(layer);
        }

        private void OnLayersChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Just in case, if need to handle extra stuff.
        }

    }
}
