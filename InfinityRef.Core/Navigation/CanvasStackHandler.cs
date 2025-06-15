using InfinityRef.Core.Models;

namespace InfinityRef.Core.Navigation
{
    public class CanvasStackHandler
    {
        private readonly Stack<Canvas> _stack = new();

        public void Push(Canvas canvas) => _stack.Push(canvas);

        public Canvas? Pop() => _stack.Count > 0 ? _stack.Pop() : null;

        public Canvas? Peek() => _stack.Count > 0 ? _stack.Peek() : null;
    }
}
