using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Toe;
using WindowsInput;

namespace Rayman2FunBox
{
    static class RandomizeRaymanMode
    {
        public static void RandomizeRaymanModeThread(MainWindow w)
        {
            int processHandle = w.GetRayman2ProcessHandle();

            //int off_cameraTarget = Memory.GetPointerPath(processHandle, Constants.off_cameraArrayPointer, 0, 0x4, 0x10, 0xc) + 0x68;
            //int off_targetFamily = Memory.GetPointerPath(processHandle, off_cameraTarget, 0x4, 0x0, 0x14);
            //int familyIndex = Memory.ReadProcessMemoryInt32(processHandle, off_targetFamily + 0xC);

            List<Dictionary<int, float>> vertOffsets = null;

            //Dictionary<int, float> bodyPartVertOffsetsOriginals = new Dictionary<int, float>(bodyPartVertOffsets);

            float timer = 0;

            Random seedRand = new Random();
            int seed = seedRand.Next();

            float clumpFactor = 1.0f;

            while (w.randomizeRaymanModeEnabled)
            {
                int engineMode = Memory.ReadProcessMemoryByte(processHandle, 0x500380); // engineMode

                if (engineMode != 9 && engineMode != 8) {
                    
                    if (vertOffsets != null) {
                        
                        // restore verts for body parts
                        foreach (var verts in vertOffsets) {
                            foreach (int offset in verts.Keys) {

                                Memory.WriteProcessMemoryFloat(processHandle, offset, verts[offset]); // restore original value
                            }
                        }

                        vertOffsets.Clear();
                        vertOffsets = null;
                    }
                    continue;
                }

                if (vertOffsets == null) {

                    vertOffsets = new List<Dictionary<int, float>>();

                    int[] families = Utils.GetFamilies(processHandle);
                    for (int fi = 0; fi < families.Length; fi++) {

                        vertOffsets.Add(Utils.GetFamilyPOVertOffsets(processHandle, families[fi], true, new int[] { })); // get all vertices                
                    }
                }

                foreach (var verts in vertOffsets) {
                    foreach (int offset in verts.Keys) {

                        engineMode = Memory.ReadProcessMemoryByte(processHandle, 0x500380); // check engine mode before writing
                        if (engineMode !=8 && engineMode != 9) {
                            continue; // go back to start of loop
                        }

                        float originalVal = verts[offset];
                        Random rand = new Random(seed + (int)(originalVal / clumpFactor));

                        // corrupt 33% of offsets
                        if (rand.NextDouble() > 0.33f) {
                            continue;
                        }

                        float checkDivision = 0.3f + (float)rand.NextDouble() * 20.0f;
                        float checkOffsetFactor = (float)rand.NextDouble() * 20.0f;
                        float checkSine = ((float)Math.Sin(timer / checkDivision + (offset * checkOffsetFactor)) + 1.0f) / 2.0f;
                        float checkVal = (float)rand.NextDouble();

                        if (checkSine < checkVal) {
                            continue;
                        }

                        float momDivision = 0.5f + (float)rand.NextDouble() * 13.0f;
                        float wowDivision = 0.5f + (float)rand.NextDouble() * 13.0f;
                        float momFactor = 0.03f + (float)rand.NextDouble() * 0.85f;
                        float wowFactor = (float)rand.NextDouble() * 0.01f;// 0.5f;
                        float momOffsetFactor = (float)rand.NextDouble() * 20.0f;
                        float wowOffsetFactor = (float)rand.NextDouble() * 20.0f;

                        float mom = 1 + (float)Math.Sin(timer / momDivision + (momOffsetFactor)) * momFactor;
                        float wow = (float)Math.Sin(timer / wowDivision + (wowOffsetFactor)) * wowFactor;

                        float newValue = verts[offset] * mom + wow;

                        Memory.WriteProcessMemoryFloat(processHandle, offset, newValue); // restore original value
                    }
                }

                // every 1 second
                if (timer++%20 == 0) {
                    seed = seedRand.Next();
                    clumpFactor = (float)(0.001f+seedRand.NextDouble() * 0.5f);
                }

                Thread.Sleep(15);
            }

            // restore verts for body parts
            foreach (var verts in vertOffsets) {
                foreach (int offset in verts.Keys) {
                    
                    Memory.WriteProcessMemoryFloat(processHandle, offset, verts[offset]); // restore original value
                }
            }
        }
    }
}
