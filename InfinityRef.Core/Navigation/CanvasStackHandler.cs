using InfinityRef.Core.Models;

namespace InfinityRef.Core.Navigation
{
    public class CanvasStackHandler
    {
        private readonly Stack<Canvas> stack = new();

        public void Push(Canvas canvas) => stack.Push(canvas);

        public Canvas? Pop() => stack.Count > 0 ? stack.Pop() : null;

        public Canvas? Peek() => stack.Count > 0 ? stack.Peek() : null;
    }
}
