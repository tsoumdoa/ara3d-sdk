using System.Collections.ObjectModel;

namespace Ara3D.SceneEval
{
    public class SceneEvalGraph
    {
        public ObservableCollection<SceneEvalNode> Sinks { get; } = [];
        
        public IEnumerable<SceneEvalNode> GetAllNodes()
            => Sinks.SelectMany(x => x.GetInputPath());

        public IEnumerable<SceneEvalNode> GetSources()
            => Sinks.Select(sink => sink.GetSource());

        public SceneEvalNode GetSink(SceneEvalNode node)
            => Sinks.FirstOrDefault(r => r.GetInputPath().Contains(node));

        public event EventHandler GraphInvalidated;
        public event EventHandler GraphChanged;

        public SceneEvalGraph()
        {
            Sinks.CollectionChanged += NotifyGraphChanged;
        }

        public void NotifyGraphChanged(object sender, EventArgs args)
        {
            GraphChanged?.Invoke(sender, EventArgs.Empty);
            NotifyGraphInvalidated(sender, args);
        }

        public void Clear()
        {
            Sinks.Clear();
        }

        public SceneEvalNode ReplaceSink(SceneEvalNode oldSink, object obj)
        {
            var sinkIndex = Sinks.IndexOf(oldSink);
            if (sinkIndex == -1)
                throw new Exception("Could not find sink");
            var newSink = new SceneEvalNode(this, obj);
            Sinks[sinkIndex] = newSink;
            newSink.SetInput(oldSink);
            return newSink;
        }

        /// <summary>
        /// Adds a new node as a sink. 
        /// </summary>
        public SceneEvalNode AddNodeAsSink(object obj)
        {
            var newNode = new SceneEvalNode(this, obj);
            Sinks.Add(newNode);
            return newNode;
        }

        /// <summary>
        /// Adds a new node before the current one, unless it is a source.
        /// Then it is added to the end. 
        /// </summary>
        public SceneEvalNode AddNode(SceneEvalNode sel, object obj)
        {
            if (sel.IsSource)
            {
                // Then we add it at the end.
                var sink = GetSink(sel);
                if (sink == null)
                    throw new Exception("Could not find sink");
                return ReplaceSink(sink, obj);
            }

            var newNode = new SceneEvalNode(this, obj);
            newNode.SetInput(sel.Input);
            sel.SetInput(newNode);
            return newNode;
        }

        public void NotifyGraphInvalidated(object sender, EventArgs args)
        {
            try
            {
                GraphInvalidated?.Invoke(sender, EventArgs.Empty);
            }
            catch
            {
            }
        }

        public void UpdateDownstreamCaches(SceneEvalNode node)
        {
            var sink = GetSink(node);
            if (sink == null)
                return;
            var path = sink.GetInputPath();
            if (path.Contains(node))
            {
                foreach (var local in path)
                {
                    if (local == node)
                    {
                        NotifyGraphInvalidated(this, EventArgs.Empty);
                        return;
                    }

                    local.InvalidateCache(false);
                }

                throw new Exception("Internal error did not find the node in the primary dependency path");
            }

            throw new Exception("Could not find node in graph");
        }
    }
}
