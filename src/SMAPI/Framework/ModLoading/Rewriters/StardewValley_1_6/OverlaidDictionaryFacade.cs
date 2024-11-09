using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewValley.Network;
using SObject = StardewValley.Object;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters.StardewValley_1_6;

/// <summary>Maps Stardew Valley 1.5.6's <see cref="OverlaidDictionary"/> methods to their newer form to avoid breaking older mods.</summary>
/// <remarks>This is public to support SMAPI rewriting and should never be referenced directly by mods. See remarks on <see cref="ReplaceReferencesRewriter"/> for more info.</remarks>
public class OverlaidDictionaryFacade : OverlaidDictionary, IRewriteFacade
{
    /*********
    ** Accessors
    *********/
    public new KeysCollection Keys => new(this);
    public new ValuesCollection Values => new(this);
    public new PairsCollection Pairs => new(this);


    /*********
    ** Enumerator facades
    *********/
    public struct KeysCollection : IEnumerable<Vector2>
    {
        private readonly OverlaidDictionary Dictionary;

        public KeysCollection(OverlaidDictionary dictionary)
        {
            this.Dictionary = dictionary;
        }

        public int Count()
        {
            return this.Dictionary.Length;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this.Dictionary);
        }

        IEnumerator<Vector2> IEnumerable<Vector2>.GetEnumerator()
        {
            return new Enumerator(this.Dictionary);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this.Dictionary);
        }

        public struct Enumerator : IEnumerator<Vector2>
        {
            private readonly OverlaidDictionary Dictionary;
            private Dictionary<Vector2, SObject>.KeyCollection.Enumerator CurEnumerator;

            public Vector2 Current => this.CurEnumerator.Current;
            object IEnumerator.Current => this.CurEnumerator.Current;

            public Enumerator(OverlaidDictionary dictionary)
            {
                this.Dictionary = dictionary;
                this.CurEnumerator = dictionary.Keys.GetEnumerator();
            }

            public bool MoveNext()
            {
                return this.CurEnumerator.MoveNext();
            }

            public void Dispose()
            {
                this.CurEnumerator.Dispose();
            }

            void IEnumerator.Reset()
            {
                this.CurEnumerator = this.Dictionary.Keys.GetEnumerator();
            }
        }
    }

    public struct PairsCollection : IEnumerable<KeyValuePair<Vector2, SObject>>
    {
        private readonly OverlaidDictionary Dictionary;

        public PairsCollection(OverlaidDictionary dictionary)
        {
            this.Dictionary = dictionary;
        }

        public KeyValuePair<Vector2, SObject> ElementAt(int index)
        {
            return this.Dictionary.Pairs.ElementAt(index);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this.Dictionary);
        }

        IEnumerator<KeyValuePair<Vector2, SObject>> IEnumerable<KeyValuePair<Vector2, SObject>>.GetEnumerator()
        {
            return new Enumerator(this.Dictionary);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this.Dictionary);
        }

        public struct Enumerator : IEnumerator<KeyValuePair<Vector2, SObject>>
        {
            private readonly OverlaidDictionary Dictionary;
            private IEnumerator<KeyValuePair<Vector2, SObject>> CurEnumerator;

            public KeyValuePair<Vector2, SObject> Current => this.CurEnumerator.Current;
            object IEnumerator.Current => this.CurEnumerator.Current;

            public Enumerator(OverlaidDictionary dictionary)
            {
                this.Dictionary = dictionary;
                this.CurEnumerator = dictionary.Pairs.GetEnumerator();
            }

            public bool MoveNext()
            {
                return this.CurEnumerator.MoveNext();
            }

            public void Dispose()
            {
                this.CurEnumerator.Dispose();
            }

            void IEnumerator.Reset()
            {
                this.CurEnumerator = this.Dictionary.Pairs.GetEnumerator();
            }
        }
    }

    public struct ValuesCollection : IEnumerable<SObject>
    {
        private readonly OverlaidDictionary Dictionary;

        public ValuesCollection(OverlaidDictionary dictionary)
        {
            this.Dictionary = dictionary;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this.Dictionary);
        }

        IEnumerator<SObject> IEnumerable<SObject>.GetEnumerator()
        {
            return new Enumerator(this.Dictionary);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this.Dictionary);
        }

        public struct Enumerator : IEnumerator<SObject>
        {
            private readonly OverlaidDictionary Dictionary;
            private Dictionary<Vector2, SObject>.ValueCollection.Enumerator CurEnumerator;

            public SObject Current => this.CurEnumerator.Current;
            object IEnumerator.Current => this.CurEnumerator.Current;

            public Enumerator(OverlaidDictionary dictionary)
            {
                this.Dictionary = dictionary;
                this.CurEnumerator = dictionary.Values.GetEnumerator();
            }

            public bool MoveNext()
            {
                return this.CurEnumerator.MoveNext();
            }

            public void Dispose()
            {
                this.CurEnumerator.Dispose();
            }

            void IEnumerator.Reset()
            {
                this.CurEnumerator = this.Dictionary.Values.GetEnumerator();
            }
        }
    }


    /*********
    ** Private methods
    *********/
    private OverlaidDictionaryFacade()
        : base(null, null)
    {
        RewriteHelper.ThrowFakeConstructorCalled();
    }
}
