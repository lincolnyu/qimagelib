namespace ImageCompLibWin.Tasking
{
    public interface IResource
    {
        /// <summary>
        ///  Number of tasks that are going to use it
        /// </summary>
        int HoldCount { get; set; }

        /// <summary>
        ///  Number of tasks that are using it
        /// </summary>
        int ReferenceCount { get; set; }

        /// <summary>
        ///  If the resource is engaged (with task manager) or not (including error state)
        /// </summary>
        /// <remarks>
        ///  When HoldCount or ReferenceCount is non-zero
        ///  this MUST BE true and this might still be true
        ///  if they both are zero
        ///  The implementation of the setter this property should
        ///  allocate and deallocate (release) the resource according
        ///  to the value of the property
        /// </remarks>
        bool IsEngaged { get; set; }

        /// <summary>
        ///  The size required for the resource to be available
        /// </summary>
        int Size { get; }
    }
}
