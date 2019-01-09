using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rayman2FunBox
{
    static class Utils
    {

        public static string GetCurrentLevelName(int processHandle)
        {
            int bytesReadOrWritten = 0;
            byte[] buffer = new byte[16];
            Memory.ReadProcessMemory((int)processHandle, Constants.off_levelName, buffer, buffer.Length, ref bytesReadOrWritten);
            string levelName = Encoding.ASCII.GetString(buffer);
            levelName = levelName.Substring(0, levelName.IndexOf((char)0)); // remove after null terminator
            return levelName;
        }

        public static  Dictionary<int, float> GetFamilyPOVertOffsets(int processHandle, int offsetFamily, bool keepInstead, params int[] indicesToRemoveOrKeep)
        {
            Dictionary<int, float> result = new Dictionary<int, float>();

            int off_defaultObjectsTable = Memory.GetPointerPath(processHandle, offsetFamily + 0x1C); // default objects table
            int firstEntry = Memory.ReadProcessMemoryInt32(processHandle, off_defaultObjectsTable + 4); // objlist_start
            int numEntries = Memory.ReadProcessMemoryInt32(processHandle, off_defaultObjectsTable + 0xC); // num entries

            int[] entryList = new int[numEntries];
            for (int i = 0; i < numEntries; i++)
            {

                entryList[i] = firstEntry + (i * 0x14);

                if (indicesToRemoveOrKeep.Contains(i) == keepInstead)
                {
                    continue;
                }

                int off_po = Memory.ReadProcessMemoryInt32(processHandle, entryList[i] + 4); // read scaleVec
                int off_visualset = Memory.ReadProcessMemoryInt32(processHandle, off_po);
                int numOfLOD = Memory.ReadProcessMemoryInt16(processHandle, off_visualset + 4);
                int visualType = Memory.ReadProcessMemoryInt16(processHandle, off_visualset + 6);

                if (numOfLOD > 0 && visualType == 0)
                {
                    int off_dataOffsetsStart = Memory.ReadProcessMemoryInt32(processHandle, off_visualset + 0xC);
                    int off_firstMesh = Memory.ReadProcessMemoryInt32(processHandle, off_dataOffsetsStart);
                    int off_firstMeshNumVertices = off_firstMesh + 0x2C;
                    int off_firstMeshNumSubBlocks = off_firstMesh + 0x2E;
                    int off_firstMeshSubBlocks = off_firstMesh + 0x14;
                    int off_firstMeshSubBlockTypes = off_firstMesh + 0x10;

                    int numSubBlocks = Memory.ReadProcessMemoryInt16(processHandle, off_firstMeshNumSubBlocks);

                    int off_verts = Memory.ReadProcessMemoryInt32(processHandle, off_firstMesh + 0);
                    short numVerts = Memory.ReadProcessMemoryInt16(processHandle, off_firstMeshNumVertices);

                    for (int vi = 0; vi < numVerts; vi++)
                    {
                        int off_vert_iter = off_verts + vi * 0xC;

                        float vx = Memory.ReadProcessMemoryFloat(processHandle, off_vert_iter + 0); // vert.x
                        float vy = Memory.ReadProcessMemoryFloat(processHandle, off_vert_iter + 4); // vert.y
                        float vz = Memory.ReadProcessMemoryFloat(processHandle, off_vert_iter + 8); // vert.z

                        if (!result.ContainsKey(off_vert_iter) && !result.ContainsKey(off_vert_iter+4) && !result.ContainsKey(off_vert_iter+8))
                        {
                            result.Add(off_vert_iter, vx);
                            result.Add(off_vert_iter + 4, vy);
                            result.Add(off_vert_iter + 8, vz);
                        }

                    }
                }
            }

            return result;
        }

        public static string[] ReadObjectNamesTable(int processHandle, int off_names_first, uint num_names)
        {
            int currentOffset = off_names_first;
            string[] names = new string[num_names];

            for (int j = 0; j < num_names; j++)
            {

                int off_names_next = Memory.ReadProcessMemoryInt32(processHandle, currentOffset);
                int off_name = Memory.ReadProcessMemoryInt32(processHandle, currentOffset + 0xC);

                names[j] = Memory.ReadProcessMemoryString(processHandle, off_name, 64);

                if (off_names_next != 0)
                {
                    currentOffset = off_names_next;
                }
            }

            return names;
        }

        public static int[] GetFamilies(int processHandle)
        {
            int off_families = 0x00500560;
            int off_head = Memory.ReadProcessMemoryInt32(processHandle, off_families);
            int numEntries = Memory.ReadProcessMemoryInt32(processHandle, off_families + 8);

            int[] result = new int[numEntries];

            int off_next = off_head;

            for(int i=0;i<numEntries;i++) {

                int element = off_next;
                off_next = Memory.ReadProcessMemoryInt32(processHandle, off_next);

                result[i] = element;
            }

            return result;
        }

        public static Dictionary<int, string[]> ReadObjectTypes(int processHandle)
        {
            Dictionary<int, string[]> objectTypes = new Dictionary<int, string[]>();
            for (int i = 0; i < 3; i++)
            {
                int off_names_header = Constants.off_objectTypes + (i * 12);
                int off_names_first = Memory.ReadProcessMemoryInt32(processHandle, off_names_header);
                int off_names_last = Memory.ReadProcessMemoryInt32(processHandle, off_names_header + 4);
                uint num_names = Memory.ReadProcessMemoryUInt32(processHandle, off_names_header + 8);

                objectTypes[i] = ReadObjectNamesTable(processHandle, off_names_first, num_names);
            }

            return objectTypes;
        }

        public static Dictionary<string, int> GetActiveSuperObjectNames(int processHandle, string[] objectNames, int superObject = 0)
        {

            Dictionary<string, int> result = new Dictionary<string, int>();

            if (superObject == 0)
            {
                int off_dynamWorld = 0x0500FD0;
                superObject = Memory.GetPointerPath(processHandle, off_dynamWorld, 8);
            }

            int nextBrother = superObject;

            do
            {
                
                if (nextBrother != 0)
                {

                    int data = Memory.ReadProcessMemoryInt32(processHandle, nextBrother + 4);
                    if (data != 0)
                    {
                        int off_stdGame = Memory.ReadProcessMemoryInt32(processHandle, data + 4);
                        int nameIndex = Memory.ReadProcessMemoryInt32(processHandle, off_stdGame + 8);
                        string name = "unknown_" + nextBrother;
                        if (nameIndex >= 0 && nameIndex < objectNames.Length)
                        {
                            name = objectNames[nameIndex];
                        }

                        if (!result.ContainsKey(name))
                        {
                            result.Add(name, nextBrother);
                        }
                    }
                }

                nextBrother = Memory.ReadProcessMemoryInt32(processHandle, nextBrother + 0x14);

            } while (nextBrother != 0);

            return result;
        }

        public static Dictionary<string, List<int>> GetActiveSuperObjectAIModelNames(int processHandle, string[] aiModelNames, int superObject = 0)
        {

            Dictionary<string, List<int>> result = new Dictionary<string, List<int>>();

            if (superObject == 0)
            {
                int off_dynamWorld = 0x0500FD0;
                superObject = Memory.GetPointerPath(processHandle, off_dynamWorld, 8);
            }

            int nextBrother = superObject;

            do
            {

                //int child = Memory.ReadProcessMemoryInt32(processHandle, superObject + );

                if (nextBrother != 0)
                {

                    int data = Memory.ReadProcessMemoryInt32(processHandle, nextBrother + 4);
                    if (data != 0)
                    {
                        int off_stdGame = Memory.ReadProcessMemoryInt32(processHandle, data + 4);
                        int nameIndex = Memory.ReadProcessMemoryInt32(processHandle, off_stdGame + 4);
                        string name = "unknown_" + nextBrother;
                        if (nameIndex >= 0 && nameIndex < aiModelNames.Length)
                        {
                            name = aiModelNames[nameIndex];
                        }

                        if (!result.ContainsKey(name))
                        {
                            result.Add(name, new List<int>() { nextBrother });
                        }
                        else
                        {
                            result[name].Add(nextBrother); // add instance
                        }
                    }
                }

                nextBrother = Memory.ReadProcessMemoryInt32(processHandle, nextBrother + 0x14);

            } while (nextBrother != 0);

            return result;
        }

        public static int GetMind(int processHandle, int superObject)
        {
            return Memory.GetPointerPath(processHandle, superObject + 4, 0xC, 0); // I am the yeast of thoughts and mind!
        }

        public static int GetActiveNormalBehavior(int processHandle, int superObject)
        {
            int off_mind = GetMind(processHandle, superObject);
            return Memory.GetPointerPath(processHandle, off_mind + 4, 0x8);
        }

        public static int GetActiveReflexBehavior(int processHandle, int superObject)
        {
            int off_mind = GetMind(processHandle, superObject);
            return Memory.GetPointerPath(processHandle, off_mind + 8, 0x8);
        }

        public static int GetAIModel(int processHandle, int superObject)
        {
            return Memory.GetPointerPath(processHandle, superObject + 4, 0xC, 0, 0);
        }

        public static int GetAIModelNormalBehaviorsPointer(int processHandle, int superObject)
        {
            int aiModel = GetAIModel(processHandle, superObject);
            return Memory.ReadProcessMemoryInt32(processHandle, aiModel);
        }

        public static int[] GetAIModelNormalBehaviorsList(int processHandle, int superObject)
        {
            int offset = GetAIModelNormalBehaviorsPointer(processHandle, superObject);
            int off_firstEntry = Memory.ReadProcessMemoryInt32(processHandle, offset);
            int numEntries = Memory.ReadProcessMemoryInt32(processHandle, offset + 0x4);

            int[] result = new int[numEntries];

            for (int i = 0; i < numEntries; i++)
            {
                result[i] = off_firstEntry + 12 * i; // each entry takes up 12 bytes
            }

            return result;
        }
    }
}
