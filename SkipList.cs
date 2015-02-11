using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Diagnostics;

namespace IndexSkipList
{
    class SkipList : IDisposable
    {
        private const int LevelPosition = 0;
        private const int ScorePosition = LevelPosition + sizeof(int);
        private const int PointersPosition = ScorePosition + sizeof(long);

        class SkipListData
        {
            public int Level { get; set; }
            public long Score { get; set; }
            public int[] Pointers { get; set; }
        }

        private const double Probability = 0.5;
        private int LastPosition = 0;
        private int MaxLevel;
        private MemoryMappedFile DataFile;
        private MemoryMappedViewAccessor DataPointer;
        private MemoryMappedFile MetaFile;
        private MemoryMappedViewAccessor MetaPointer;
        private Random Rand;


        public SkipList(string filename, int maxLevel = 20)
        {
            string metaFile = filename + ".meta";

            MaxLevel = maxLevel;

            Rand = new Random();

            // 빈파일 생성
            if (File.Exists(filename) == false)
            {
                byte[] emptyData = new byte[] { 0, 0, 0, 0 };

                var fp = File.Open(filename, FileMode.CreateNew, FileAccess.Write);

                for (int i = 0; i < 10000000; i++)
                {
                    fp.Write(emptyData, 0, 4);
                }
                fp.Close();

                fp = File.Open(metaFile, FileMode.CreateNew, FileAccess.Write);

                for (int i = 0; i < 262144; i++)
                {
                    fp.Write(emptyData, 0, 4);
                }
                fp.Close();

                Console.WriteLine("write end");
            }

            DataFile = MemoryMappedFile.CreateFromFile(filename);
            DataPointer = DataFile.CreateViewAccessor();

            InitList();

            for (int i = 0; i < 50; i++)
            {
                AddScore(Rand.Next());

                if (i % 100 == 0 && i > 0 )
                {
                    Console.WriteLine(i);
                }
            }

        }

        public void Dispose()
        {
            Console.Write("dispose");
        }

        public void PrintList()
        {
            int position = 0;
            var data = ReadData(position);
            while (true)
            {
                Console.WriteLine("{0} {1}", data.Score, position);

                position = data.Pointers[0];
                if (position >= LastPosition || position == 0)
                {
                    break;
                }

                data = ReadData(position);
            }
        }

        private void InitList()
        {
            AddList(0, Enumerable.Range(0, MaxLevel).Select(x => 0).ToArray());
        }

        private int AddList(long score, params int[] pointers)
        {
            int startPosition = LastPosition;
            int position = startPosition;

            DataPointer.Write(position, (int)pointers.Length);
            position += sizeof(int);

            DataPointer.Write(position, score);
            position += sizeof(long);

            foreach (var pointer in pointers)
            {
                DataPointer.Write(position, (int)pointer);
                position += sizeof(int);
            }

            LastPosition = position;

            return startPosition;
        }

        private int RandomLevel()
        {
            int level = 1;

            // Determines the next node level.
            while (Rand.NextDouble() < Probability && level < MaxLevel)
            {
                level++;
            }

            return level;
        }

        private SkipListData ReadData(int position)
        {

            var data = new SkipListData();


            data.Level = DataPointer.ReadInt32(position);
            position += sizeof(int);

            data.Score = DataPointer.ReadInt64(position);
            position += sizeof(long);

            List<int> datas = new List<int>();
            for (int i = 0; i < data.Level; i++)
            {
                datas.Add(DataPointer.ReadInt32(position));
                position += 4;
            }

            data.Pointers = datas.ToArray();
            //data.Pointers = Enumerable.Range(0, data.Level).Select(x => BR.ReadInt32()).ToArray();

            return data;
        }

        public List<Tuple<int, int>> FIndByScore(long score, int verbose = 0, bool displayVerbose = false)
        {
            var results = new List<Tuple<int, int>>();

            for (int i = 0; i < MaxLevel; i++)
            {
                results.Add(new Tuple<int, int>(0, 0));
            }

            int position = 0;

            verbose++;
            var data = ReadData(position);
            int level = MaxLevel - 1;

            while (true)
            {
                int nextPosition = data.Pointers[level];
                if (nextPosition == 0)
                {
                    goto levelDown;
                }

                var nextData = ReadData(nextPosition);
                verbose++;

                if (nextData.Score < score)
                {
                    data = nextData;
                    position = nextPosition;
                    results[level] = new Tuple<int, int>(position, data.Pointers[level]);
                }
                else if (nextData.Score == score)
                {
                    for (int i = 0; i <= level; i++)
                    {
                        results[level] = new Tuple<int, int>(position, data.Pointers[level]);
                    }
                    if ( displayVerbose == true)
                    {
                        Console.WriteLine(verbose);
                    }
                    return results;
                }
                else /* if ( nextData.Score > score) */
                {
                    goto levelDown;
                }

                continue;

            levelDown:
                results[level] = new Tuple<int, int>(position, data.Pointers[level]);
                level--;
                if (level < 0)
                {
                    if (displayVerbose == true)
                    {
                        Console.WriteLine(verbose);
                    }

                    return results;
                }
                continue;

            }
        }

        public void AddScore(long score)
        {
            int level = RandomLevel();
            var findResult = FIndByScore(score);

            var position = AddList(score, findResult.GetRange(0, level).Select(x => x.Item2).ToArray());

            for (int i = 0; i < level; i++)
            {
                ModifyPointer(findResult[i].Item1, i, position);
            }
        }

        private void ModifyPointer(int position, int index, int pointer)
        {
            int pointerPosition = position + PointersPosition + sizeof(int) * index;
            DataPointer.Write(pointerPosition, (int)pointer);
        }
    }
}
