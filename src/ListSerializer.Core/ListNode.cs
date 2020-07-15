namespace ListSerializer.Core
{
    /// <summary>
    /// Node of the list with random links
    /// </summary>
    public class ListNode
    {
        /// <summary>
        /// Ref to the previous node in the list, null for head
        /// </summary>
        public ListNode Previous;

        /// <summary>
        /// Ref to the next node in the list, null for tail
        /// </summary>
        public ListNode Next;

        /// <summary>
        /// Ref to the random node in the list, could be null
        /// </summary>
        public ListNode Random;

        /// <summary>
        /// Payload
        /// </summary>
        public string Data;
    }
}