namespace Lattice.LoadGenerator
{
    using System;

    /// <summary>
    /// A collection archetype: a name, a human-readable description, and a factory that produces one
    /// realistic JSON document for the collection.
    /// </summary>
    public class CollectionTheme
    {
        #region Public-Members

        /// <summary>Collection name.</summary>
        public string Name { get; }

        /// <summary>Collection description.</summary>
        public string Description { get; }

        /// <summary>Factory producing a single JSON document for this theme.</summary>
        public Func<string> DocumentFactory { get; }

        #endregion

        #region Constructors-and-Factories

        /// <summary>Instantiate.</summary>
        /// <param name="name">Collection name.</param>
        /// <param name="description">Collection description.</param>
        /// <param name="documentFactory">Factory producing a single JSON document.</param>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        public CollectionTheme(string name, string description, Func<string> documentFactory)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            DocumentFactory = documentFactory ?? throw new ArgumentNullException(nameof(documentFactory));
        }

        #endregion
    }
}
