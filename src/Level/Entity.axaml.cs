using Kaitai;
using System;
using System.Collections.Generic;
using SMM2Level.Utility;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SMM2Level
{
    public partial class Entity : UserControl
    {
        public virtual byte[] GetBytes() 
        { 
            throw new NotImplementedException();
        }
        public virtual void LoadFromStream(KaitaiStream io, Canvas? canvas = null) 
        { 
            throw new NotImplementedException();
        }

        public Entity()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static void FillLists<T>(List<T> entities, int numEntities, KaitaiStream io, Canvas? canvas = null) where T : Entity, new()
        {
            entities = new List<T>(numEntities);

            for (int i = 0; i < numEntities; i++)
            {
                T entity = new();
                canvas?.Children.Add(entity);

                entity.LoadFromStream(io, canvas);

                entities.Add(entity);
            }

            // toss remaining data
            // TODO: REPLACE WITH ARITHMETIC
            // io.ReadBytes(sizeof(T) * (maxEntities - numEntities));
            string entityName = typeof(T).Name;
            int maxEntities = (int)Enum.Parse(typeof(Maxes), entityName);
            int sizeOfEntity = (int)Enum.Parse(typeof(Sizes), entityName);

            // i dont think this works rn because it needs to count the sub-object size

            io.ReadBytes(sizeOfEntity * Math.Max(maxEntities - numEntities, 0));
        }

        public static byte[] GetBytesFromList<T>(List<T> entities) where T : Entity, new()
        {
            string entityName = typeof(T).Name;
            int maxEntities = (int)Enum.Parse(typeof(Maxes), entityName);

            ByteBuffer bb = new ByteBuffer();

            int numEntities = Math.Min(entities.Count, maxEntities);

            for ( int i = 0; i < numEntities; i++)
            {
                bb.Append(entities[i].GetBytes());
            }

            // append garbage data
            T entity = new();
            for (int i = numEntities; i < maxEntities; i++)
            {
                bb.Append(entity.GetBytes());
            }

            return bb.GetBytes();
        }
    }
}
