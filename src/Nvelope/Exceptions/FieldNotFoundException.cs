//-----------------------------------------------------------------------
// <copyright file="FieldNotFoundException.cs" company="TWU">
// MIT Licenced
// </copyright>
//-----------------------------------------------------------------------

namespace Nvelope.Exceptions
{
    using System;

    /// <summary>
    /// Indicates that reflection couldn't find a field
    /// </summary>
    public class FieldNotFoundException : Exception
    {
        #region These are just here for FxCop rules

        /// <summary>
        /// Initializes a new instance of the FieldNotFoundException class.
        /// The instance will be empty.
        /// </summary>
        public FieldNotFoundException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the FieldNotFoundException class.
        /// Important properties in the instance will be empty.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        public FieldNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FieldNotFoundException class.
        /// Important properties in the instance will be empty.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the
        /// current exception, or a null reference if no inner exception is specified.</param>
        public FieldNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }


        #endregion
    }
}
