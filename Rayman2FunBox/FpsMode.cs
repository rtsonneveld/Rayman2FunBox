using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Toe;
using WindowsInput;

namespace Rayman2FunBox
{
    static class FpsMode
    {
        public static void FpsModeThread(MainWindow w)
        {
            int processHandle = w.GetRayman2ProcessHandle();

            int bytesReadOrWritten = 0; // Required somehow

            byte[] buffer = new byte[4];

            int off_DNM_p_stDynamicsCameraMechanics = 0x4359D0; // original byte = 81, replaced by ret = C3
            int off_ForceCameraPos = 0x473420; // original byte = 53, replaced by ret = C3
            int off_ForceCameraTgt = 0x473480; // original byte = 83, replaced by ret = C3

            byte[] fixePositionPersoHackOriginal = new byte[] { 0x74, 0x16, 0x6A, 0x00, 0x6A, 0x28, 0xE8, 0x39, 0x50, 0x01, 0x00 };
            byte[] fixePositionPersoHackModified = new byte[] { 0x90, 0x90, 0x6A, 0x90, 0x6A, 0x28, 0x90, 0x90, 0x90, 0x90, 0x90 };

            buffer = new byte[] { 0xC3 };
            Memory.WriteProcessMemory(processHandle, off_DNM_p_stDynamicsCameraMechanics, buffer, buffer.Length, ref bytesReadOrWritten);
            Memory.WriteProcessMemory(processHandle, off_ForceCameraPos, buffer, buffer.Length, ref bytesReadOrWritten); Memory.WriteProcessMemory(processHandle, off_DNM_p_stDynamicsCameraMechanics, buffer, buffer.Length, ref bytesReadOrWritten);
            Memory.WriteProcessMemory(processHandle, off_ForceCameraTgt, buffer, buffer.Length, ref bytesReadOrWritten);

            int off_cameraSPO = Memory.GetPointerPath(processHandle, Constants.off_cameraArrayPointer, 0);
            int off_cameraMatrix = Memory.GetPointerPath(processHandle, Constants.off_cameraArrayPointer, 0, 0x20);

            int off_raymanCustomBits = Memory.GetPointerPath(processHandle, Constants.off_mainChar, 4, 4) + 0x24;
            int off_raymanGravity = Memory.GetPointerPath(processHandle, Constants.off_mainChar, 4, 8, 0) + 0x10;
            int off_raymanDsgVar16 = Memory.GetPointerPath(processHandle, Constants.off_mainChar, 4, 0xC, 0, 0xC, 8) + 0x203; // rayman dsg var 16 indicates if he can be controlled
            int off_cameraFOV = Memory.GetPointerPath(processHandle, Constants.off_cameraArrayPointer, 0, 0x4, 0x10, 0x4) + 0x5c;

            int off_raymanDynamicsMatrix = Memory.GetPointerPath((int)processHandle, 0x500578, 0x4, 0x8, 0x0) + 0x7c;

            int off_cameraPerso = Memory.GetPointerPath(processHandle, Constants.off_cameraArrayPointer, 0, 0x4);
            int off_cameraRule = Memory.GetPointerPath(processHandle, Constants.off_cameraArrayPointer, 0, 0x4, 0xC, 0) + 0x4; // brain.mind.intelligenceNormal

            buffer = new byte[] { 0, 0, 0, 0 };
            //Memory.WriteProcessMemory(processHandle, off_cameraRule, buffer, buffer.Length, ref bytesReadOrWritten);

            //byte[] cameraMatrix = new byte[;
            byte[] matrixBuffer = new byte[188];

            Matrix matrix;

            bool tempDisable = false;

            int off_cameraTarget = Memory.GetPointerPath(processHandle, Constants.off_cameraArrayPointer, 0, 0x4, 0x10, 0xc) + 0x68;
            int off_targetFamily = Memory.GetPointerPath(processHandle, off_cameraTarget, 0x4, 0x0, 0x14);
            int familyIndex = Memory.ReadProcessMemoryInt32(processHandle, off_targetFamily + 0xC);

            int raymanSPO = Memory.ReadProcessMemoryInt32(processHandle, off_cameraTarget);

            int[] raymanBehaviors = Utils.GetAIModelNormalBehaviorsList(processHandle, raymanSPO);

            Dictionary<int, float> bodyPartVertOffsets = new Dictionary<int, float>();
            if (familyIndex == 0)
            {
                bodyPartVertOffsets = Utils.GetFamilyPOVertOffsets(processHandle, off_targetFamily, true, new int[] { 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 18, 19, 20, 21, 22, 23, 24, 25 });
            } else
            {
                w.fpsModeEnabled = false;
                MessageBox.Show("You can only start fps mode while you are in game and playing as Rayman.");
            }

            bool bodyPartsHidden = false;

            var objectTypes = Utils.ReadObjectTypes(processHandle);

            string lastLevelName = "";
            string levelName = "";

            int dockingTimer = 0;
            int waitForDockedTimer = 0;
            int dockedTimer = 0;
            float dockingRotation = 0;
            float dockingOffset = 0;
            int dockingCooldownTimer = 0;

            int levelTimer = 0;

            int off_cameraTargetX = Memory.GetPointerPath(processHandle, Constants.off_cameraArrayPointer, 0, 4, 8, 4) + 0x10;
            int off_cameraTargetY = Memory.GetPointerPath(processHandle, Constants.off_cameraArrayPointer, 0, 4, 8, 4) + 0x14;
            int off_cameraTargetZ = Memory.GetPointerPath(processHandle, Constants.off_cameraArrayPointer, 0, 4, 8, 4) + 0x18;

            int umberTimer = 0;

            float pitch = 0;
            float yaw = 0;

            float climbingOffsetY = 0;
            float climbingOffsetZ = 0;
            float climbingPitch = 0;
            float climbingYaw = 0;

            float bobbin = 0;

            Quaternion rotation = new Quaternion();
            Quaternion interpolatedRotation = new Quaternion();

            float backStrafeOffset = 0;

            bool downHeld = false;

            var inputSim = new InputSimulator();

            float carryingFloat = 0; // smooth object carrying animations
            float barrelFlyingFloat = 0; // smooth barrel flying animations

            while (w.fpsModeEnabled)
            {

                int engineMode = Memory.ReadProcessMemoryByte(processHandle, 0x500380); // engineMode

                bool frozen = Memory.ReadProcessMemoryByte(processHandle, 0x500FAA) > 0 ? true : false; // unfreeze game while thread sleeps

                if (engineMode != 9 && engineMode != 8)
                {
                    levelTimer = 0;
                    continue; // ignore
                }

                var activeSuperObjects = Utils.GetActiveSuperObjectNames(processHandle, objectTypes[2]);
                var activeSuperObjectsAIModelNames = Utils.GetActiveSuperObjectAIModelNames(processHandle, objectTypes[1]);

                levelTimer++;

                levelName = Utils.GetCurrentLevelName(processHandle);

                if (lastLevelName != levelName && off_cameraTarget != 0)
                {
                    lastLevelName = levelName;
                    objectTypes = Utils.ReadObjectTypes(processHandle);
                }

                Vector3 raymanSpeed = new Vector3(
                    Memory.ReadProcessMemoryFloat(processHandle, off_raymanGravity + 0x2C),
                    Memory.ReadProcessMemoryFloat(processHandle, off_raymanGravity + 0x30),
                    Memory.ReadProcessMemoryFloat(processHandle, off_raymanGravity + 0x34)
                );

                Vector3 raymanSpeedRotated = new Vector3(raymanSpeed);
                Vector3 rotationEulers = Matrix.QuaternionToEuler(rotation); ;
                raymanSpeedRotated.X = (float)(raymanSpeed.X * Math.Cos(rotationEulers.Z) - raymanSpeed.Y * Math.Sin(rotationEulers.Z));
                raymanSpeedRotated.Y = (float)(raymanSpeed.X * Math.Sin(rotationEulers.Z) + raymanSpeed.Y * Math.Cos(rotationEulers.Z));

                uint raymanFlags = Memory.ReadProcessMemoryUInt32(processHandle, off_raymanGravity - 8);
                uint raymanFlags2 = Memory.ReadProcessMemoryUInt32(processHandle, off_raymanGravity - 4);
                float gravity = Memory.ReadProcessMemoryFloat(processHandle, off_raymanGravity);
                bool climbing = (raymanFlags & (1 << 6)) != 0;
                bool onGround = (raymanFlags & (1 << 5)) != 0;
                bool sliding = (raymanFlags2 & (1 << 21)) != 0;

                off_cameraTarget = Memory.GetPointerPath(processHandle, Constants.off_cameraArrayPointer, 0, 0x4, 0x10, 0xc) + 0x68;
                int off_raymanMatrix = Memory.GetPointerPath(processHandle, off_cameraTarget, 0x20);
                off_targetFamily = Memory.GetPointerPath(processHandle, off_cameraTarget, 0x4, 0x0, 0x14);

                familyIndex = Memory.ReadProcessMemoryInt32(processHandle, off_targetFamily + 0xC);
                string familyName = objectTypes[0][familyIndex];

                string[] allowedFamilies =
                {
                    "rayman",
                    "obus",
                    "new_chaise_russe",
                    "Rayman_ski",
                    "nef_pirate"
                };

                // If not one of allowed families, assume main character instead of camera target
                if (!allowedFamilies.Contains(familyName))
                {
                    off_raymanMatrix = Memory.GetPointerPath(processHandle, Constants.off_mainChar, 0x20);
                }

                uint raymanCustomBits = Memory.ReadProcessMemoryUInt32(processHandle, off_raymanCustomBits);
                byte raymanDsgVar16 = Memory.ReadProcessMemoryByte(processHandle, off_raymanDsgVar16);

                tempDisable = false;

                if (levelName.ToLower() == "mapmonde" || levelName.ToLower() == "raycap" || levelName.ToLower() == "menu" || levelName.ToLower() == "staff_10" || levelName.ToLower() == "end_10")
                {
                    tempDisable = true;
                }

                bool noPitchSmoothing = false;
                bool inBush = false;

                // Rayman custom bit 16 = no control
                if ((raymanCustomBits & (1 << 16)) != 0)
                {
                    tempDisable = true;

                    if (activeSuperObjects.ContainsKey("CCC_Buisson"))
                    {

                        float x = Memory.ReadProcessMemoryFloat(processHandle, off_raymanMatrix + 4);

                        if (x < 16878)
                        {
                            tempDisable = false;
                            inBush = true;
                        }
                    }
                }

                // Rayman dsg var 16 set = control
                if (raymanDsgVar16 == 0)
                {
                    tempDisable = true;
                }

                // If an object with AI model ARG_Rayseau exists, this is a rayman dummy that should be used as camera viewpoint
                if (activeSuperObjectsAIModelNames.ContainsKey("ARG_Rayseau") && levelName!="bast_20") {

                    int dummyRaymanObject = activeSuperObjectsAIModelNames["ARG_Rayseau"][0];
                    int off_dummyCustomBits = Memory.GetPointerPath(processHandle, dummyRaymanObject + 4, 4) + 0x24;
                    int dummyBits = Memory.ReadProcessMemoryInt32(processHandle, off_dummyCustomBits);

                    // bit 16 disabled
                    if ((dummyBits & (1 << 16)) == 0) {
                        tempDisable = false;
                        off_raymanMatrix = Memory.GetPointerPath(processHandle, dummyRaymanObject + 0x20);
                    }
                }

                if (familyName == "nef_pirate")
                {
                    tempDisable = false;
                }

                if (levelName.ToLower() == "vulca_10")
                {
                    if (levelTimer < 10)
                    { // disable at start
                        tempDisable = true;
                    }
                }

                if (levelName.ToLower() == "vulca_20")
                {
                    float z = Memory.ReadProcessMemoryFloat(processHandle, off_raymanMatrix + 12);
                    if (z < -42 && z > -490)
                    {
                        tempDisable = true;
                    }
                }

                matrix = Matrix.Read(processHandle, off_raymanMatrix);
                int off_raymanParentObject = Memory.GetPointerPath(processHandle, off_cameraTarget, 0x1C);
                int raymanParentObjectType = Memory.ReadProcessMemoryInt32(processHandle, off_raymanParentObject);

                Matrix parentMatrix = null;
                if (raymanParentObjectType == 2)
                {
                    int off_parentMatrix = Memory.GetPointerPath(processHandle, 0x500578, 0x1C, 0x20);
                    parentMatrix = Matrix.Read(processHandle, off_parentMatrix);
                    matrix.m = parentMatrix.m * matrix.m;
                    noPitchSmoothing = true;
                }

                if (levelName.ToLower() == "ile_10")
                {
                    if (activeSuperObjects.ContainsKey("MIC_Freddox"))
                    {

                        int chickenAddress = activeSuperObjects["MIC_Freddox"];
                        int off_aiModel = Memory.GetPointerPath(processHandle, chickenAddress + 4, 0xC, 0);
                        int off_aiModelNormalBehaviors = Memory.ReadProcessMemoryInt32(processHandle, off_aiModel);

                        int off_scriptDistanceCheckNodeRule6 = off_aiModelNormalBehaviors + 0x8AC;
                        int off_scriptDistanceCheckNodeMacro = off_aiModel + 0x1130;
                        float distRule = Memory.ReadProcessMemoryFloat(processHandle, off_scriptDistanceCheckNodeRule6);
                        float distMacro = Memory.ReadProcessMemoryFloat(processHandle, off_scriptDistanceCheckNodeMacro);

                        Memory.WriteProcessMemoryFloat(processHandle, off_scriptDistanceCheckNodeRule6, 0);
                        Memory.WriteProcessMemoryFloat(processHandle, off_scriptDistanceCheckNodeMacro, 0);
                    }
                }

                if (levelName.ToLower() == "rodeo_60")
                {
                    // in menhir hills 3, the menhirs can shake the screen and mess up FPS mode. here we replace some code to prevent this from happening, but only between certain coordinates
                    if (activeSuperObjects.ContainsKey("OLP_Menhir_Tombe_1"))
                    {

                        int menhirAddress = activeSuperObjects["OLP_Menhir_Tombe_1"];
                        int off_aiModel = Memory.GetPointerPath(processHandle, menhirAddress + 4, 0xC, 0);
                        int off_aiModelNormalBehaviors = Memory.ReadProcessMemoryInt32(processHandle, off_aiModel);

                        int off_scriptTimeCheckNodeRule3 = off_aiModelNormalBehaviors + 0x3F8;

                        Memory.WriteProcessMemoryInt32(processHandle, off_scriptTimeCheckNodeRule3, 0);
                    }
                }

                if (levelName.ToLower() == "glob_10")
                {
                    if (activeSuperObjects.ContainsKey("ZOR_Tremble_Mat01"))
                    {
                        int trembleAddress = activeSuperObjects["ZOR_Tremble_Mat01"];

                        int off_trembleCustomBits = Memory.GetPointerPath(processHandle, trembleAddress + 4, 4) + 0x24;
                        Memory.WriteProcessMemoryInt32(processHandle, off_trembleCustomBits, 1 << 16);
                    }
                }

                Toe.Matrix4 offsetMatrix = Toe.Matrix4.CreateTranslation(0, 0, 0);
                offsetMatrix.M14 = 0f;
                offsetMatrix.M24 = 0.2f + backStrafeOffset;
                offsetMatrix.M34 = 1.5f;
                if (raymanSpeedRotated.Y > 0.5f && raymanParentObjectType != 2)
                {
                    backStrafeOffset += (0.6f - backStrafeOffset) / 5.0f;
                }
                else
                {
                    backStrafeOffset += (-backStrafeOffset) / 5.0f;
                }

                if (onGround && !sliding && familyName == "rayman")
                { // Head bobbing when walking

                    float bobbinFactor = raymanSpeed.Length / 20.0f;

                    offsetMatrix.M14 += (float)Math.Sin(bobbin) * bobbinFactor * 0.4f;
                    offsetMatrix.M34 += (float)Math.Cos(bobbin * 2) * bobbinFactor * 0.2f;

                    bobbin += 0.35f * bobbinFactor;
                    if (float.IsNaN(bobbin))
                    {
                        bobbin = 0;
                    }
                }

                if (familyName == "rayman")
                {
                    int currentBehaviorOffset = Utils.GetActiveNormalBehavior(processHandle, raymanSPO);
                    int currentBehaviorIndex = Array.IndexOf(raymanBehaviors, currentBehaviorOffset);

                    // Carrying barrel/plum/orb?
                    if (currentBehaviorIndex == 28 || currentBehaviorIndex == 29)
                    {
                        carryingFloat += (1.0f - carryingFloat) / 15.0f;
                    }
                    else
                    {
                        carryingFloat += (0.0f - carryingFloat) / 10.0f;
                    }

                    offsetMatrix.M14 += 0.9f * carryingFloat; // look a little to the left
                    offsetMatrix.M24 += 0.3f * carryingFloat; // move camera back a little
                    offsetMatrix.M34 -= 0.3f * carryingFloat; // move camera back a little

                    // Flying barrel?
                    if (currentBehaviorIndex == 19)
                    {
                        barrelFlyingFloat += (1.0f - barrelFlyingFloat) / 5.0f;
                    }
                    else
                    {
                        barrelFlyingFloat += (0.0f - barrelFlyingFloat) / 5.0f;
                    }

                    offsetMatrix.M24 -= 0.15f * barrelFlyingFloat; // move camera forward a little
                    offsetMatrix.M34 -= 0.8f * barrelFlyingFloat; // move camera down a little

                    if (activeSuperObjectsAIModelNames.ContainsKey("MIC_Obus_Complexe"))
                    {
                        if (currentBehaviorIndex == 10)
                        {
                            offsetMatrix.M34 += 1.1f;
                            offsetMatrix.M24 += 0.3f;
                        }
                    }


                    // disable fps mode in final cutscene on crow's nest
                    if (levelName.ToLower() == "rhop_10") {
                        if (currentBehaviorIndex == 10) {
                            tempDisable = true;
                        }
                    }

                    // disable fps mode at start of level of marshes 2 and start of whale bay 3
                    if (levelName.ToLower() == "ski_60") {
                        if (currentBehaviorIndex == 10) {
                            tempDisable = true;
                        }
                    }

                }

                offsetMatrix.M24 += climbingOffsetY;
                offsetMatrix.M34 += climbingOffsetZ;

                rotation = matrix.m.ExtractRotation();
                if (interpolatedRotation == null)
                {
                    interpolatedRotation = rotation;
                }

                Toe.Matrix4 rotationMatrix = Toe.Matrix4.CreateRotationZ(0);

                // Immediately update lastRotation when not on shell
                if (familyName != "obus" && !inBush)
                {
                    interpolatedRotation = rotation;
                }

                if (familyName == "obus" || inBush)
                {
                    offsetMatrix.M34 = 2.3f;
                    offsetMatrix.M24 = 0.7f;
                    noPitchSmoothing = true;

                    // smooth rotation on shell a little with quaternion interpolation
                    matrix.m = matrix.m.ClearRotation();
                    Vector3 eulers = Matrix.QuaternionToEuler(rotation);

                    interpolatedRotation = Quaternion.Slerp(interpolatedRotation, rotation, 0.25f);
                    Matrix4 interpolatedRotationMatrix = Matrix4.CreateFromQuaternion(interpolatedRotation);

                    matrix.m = matrix.m * interpolatedRotationMatrix;

                }
                else if (familyName == "new_chaise_russe")
                {
                    offsetMatrix.M34 = 2.1f;
                    offsetMatrix.M24 = 0.3f;
                    noPitchSmoothing = true;
                }
                else if (familyName == "nef_pirate")
                {
                    offsetMatrix.M34 = 12f;
                    offsetMatrix.M24 = -4f;
                    noPitchSmoothing = true;

                    dockedTimer -= 1;

                    int off_pirateCustomBits = Memory.GetPointerPath(processHandle, Constants.off_mainChar, 4, 4) + 0x24;
                    uint pirateCustomBits = Memory.ReadProcessMemoryUInt32(processHandle, off_pirateCustomBits);

                    // Pirate ship custom bit 27 = docking
                    if ((pirateCustomBits & (1 << 27)) != 0 && dockingCooldownTimer <= 0)
                    {
                        if (dockedTimer <= 0)
                        {
                            dockingTimer += 1;
                            if (dockingTimer > 15)
                            {
                                waitForDockedTimer = 60 * 2;
                                dockingTimer = 0;
                                dockingCooldownTimer = 60 * 20;
                            }
                        }
                    }
                    else
                    {
                        dockingTimer = 0;
                    }

                    dockingCooldownTimer -= 1;

                    if (waitForDockedTimer > 0)
                    {
                        waitForDockedTimer -= 1;
                        if (waitForDockedTimer == 0)
                        {
                            dockedTimer = 60 * 8;
                        }
                    }

                    if (dockedTimer > 0)
                    {
                        dockingRotation += (float)(Math.PI - dockingRotation) / 20.0f;
                        dockingOffset += (2 - dockingOffset) / 20.0f;
                    }
                    else
                    {
                        dockingRotation += (0 - dockingRotation) / 20.0f;
                        dockingOffset += (-dockingOffset) / 20.0f;
                    }

                    offsetMatrix.M24 -= dockingOffset * 5;

                    rotationMatrix = Toe.Matrix4.CreateRotationZ(dockingRotation);
                }

                matrix.m = matrix.m * offsetMatrix * rotationMatrix;

                // smooth out rotation
                if (!noPitchSmoothing)
                {
                    matrix.m = matrix.m.ClearRotation();
                    Vector3 eulers = Matrix.QuaternionToEuler(rotation);

                    // Slightly look down when helicoptering
                    if (Math.Round(gravity) == 4 || Math.Round(gravity) == 25)
                    {
                        if (raymanSpeed.Z < 0)
                        {
                            eulers.X -= 0.22f;
                        }
                    }

                    // Look down when freefalling
                    if (raymanSpeed.Z < -15 && !onGround)
                    {
                        float downAngle = (float)Math.Abs(raymanSpeed.Z + 15);
                        eulers.X -= downAngle * 0.03f;
                    }

                    pitch += Matrix.AngleDifference(pitch, eulers.X) / 10f;

                    matrix.m = matrix.m * Toe.Matrix4.CreateRotationZ(eulers.Z) * Toe.Matrix4.CreateRotationZ(eulers.Y) * Toe.Matrix4.CreateRotationX(pitch);
                }
                //

                if (climbing)
                {
                    climbingOffsetY += (0.3f - climbingOffsetY) / 8.0f;
                    climbingOffsetZ += (0.65f - climbingOffsetZ) / 8.0f;

                    // smooth yaw
                    matrix.m = matrix.m.ClearRotation();
                    Vector3 eulers = Matrix.QuaternionToEuler(rotation);

                    float pitchOffset = 1f;
                    float pitchInterp = 6f;
                    float yawOffset = 1f;
                    float yawInterp = 6f;

                    if (activeSuperObjects.ContainsKey("OLP_Generateur_DK_1"))
                    { // Fairy glade 3 barrel climb section
                        pitchOffset = 2f;
                        pitchInterp = 12f;
                        yawOffset = 0.3f;
                        yawInterp = 4f;
                    }

                    if (raymanSpeed.Z > 1f)
                    {
                        climbingPitch += (pitchOffset - climbingPitch) / pitchInterp;
                    }
                    else if (raymanSpeed.Z < -1f)
                    {
                        climbingPitch += (-pitchOffset - climbingPitch) / pitchInterp;
                    }
                    else
                    {
                        climbingPitch += (-climbingPitch) / pitchInterp * 2;
                    }

                    if (raymanSpeedRotated.X > 1f)
                    {
                        climbingYaw += (-yawOffset - climbingYaw) / yawInterp;
                    }
                    else if (raymanSpeedRotated.X < -1f)
                    {
                        climbingYaw += (yawOffset - climbingYaw) / yawInterp;
                    }
                    else
                    {
                        climbingYaw += (-climbingYaw) / yawInterp * 2;
                    }

                    eulers.X += climbingPitch;
                    eulers.Z += climbingYaw;

                    pitch += Matrix.AngleDifference(pitch, eulers.X) / 10f;
                    yaw += Matrix.AngleDifference(yaw, eulers.Z) / 10f;

                    matrix.m = matrix.m * Toe.Matrix4.CreateRotationZ(yaw) * Toe.Matrix4.CreateRotationZ(eulers.Y) * Toe.Matrix4.CreateRotationX(pitch);

                }
                else
                {
                    climbingOffsetY += (0 - climbingOffsetY) / 8.0f;
                    climbingOffsetZ += (0 - climbingOffsetZ) / 8.0f;

                    Vector3 eulers = Matrix.QuaternionToEuler(rotation);
                    yaw = eulers.Z;
                }


                if (levelName.ToLower().StartsWith("plum_10"))
                {

                    if (parentMatrix != null)
                    {
                        float platformX = parentMatrix.m.M14;
                        float platformY = parentMatrix.m.M24;
                        float platformZ = parentMatrix.m.M34;

                        Toe.Vector3 platformPos = new Toe.Vector3(parentMatrix.m.M14, parentMatrix.m.M24, parentMatrix.m.M34);
                        Toe.Vector3 checkPos = new Toe.Vector3(151.6f, 7.13f, -149.2f);
                        Toe.Vector3 diff = platformPos - checkPos;

                        if (diff.LengthSquared < 20 * 20)
                        {
                            tempDisable = true;
                            umberTimer = 60 * 2;
                        }
                    }

                }

                if (levelName.ToLower() == "mine_10") {
                    if (activeSuperObjects.ContainsKey("MOM_OLD_THX2")) {
                        int cutsceneObj = activeSuperObjects["MOM_OLD_THX2"];
                        int cutsceneBehavior = Utils.GetActiveNormalBehavior(processHandle, cutsceneObj);
                        int[] behaviorList = Utils.GetAIModelNormalBehaviorsList(processHandle, cutsceneObj);
                        int cutsceneBehaviorIndex = Array.IndexOf(behaviorList, cutsceneBehavior);
                        
                        // behavior != rule 2 (attend)
                        if (cutsceneBehaviorIndex!=5) {
                            // Disable fps mode in the ending cutscene of mine_10
                            tempDisable = true;
                        }
                    }
                }

                if (umberTimer-- > 0)
                {
                    tempDisable = true;
                }

                float inputX = Memory.ReadProcessMemoryFloat(processHandle, Constants.off_inputX);
                float inputY = Memory.ReadProcessMemoryFloat(processHandle, Constants.off_inputY);

                if (inputY > 20 && (familyName != "obus"))
                {
                    if (!downHeld)
                    {
                        downHeld = true;
                        inputSim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.RCONTROL);
                    }
                }
                else
                {
                    if (downHeld)
                    {
                        inputSim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.RCONTROL);
                        downHeld = false;
                    }
                }

                if (frozen)
                {
                    tempDisable = true;
                }

                if (tempDisable)
                { // bit 17 = take away control

                    // restore opcodes
                    buffer = new byte[] { 0x81 };
                    Memory.WriteProcessMemory(processHandle, off_DNM_p_stDynamicsCameraMechanics, buffer, buffer.Length, ref bytesReadOrWritten);
                    buffer = new byte[] { 0x53 };
                    Memory.WriteProcessMemory(processHandle, off_ForceCameraPos, buffer, buffer.Length, ref bytesReadOrWritten);
                    buffer = new byte[] { 0x83 };
                    Memory.WriteProcessMemory(processHandle, off_ForceCameraTgt, buffer, buffer.Length, ref bytesReadOrWritten);

                    // restore turn speed
                    Memory.WriteProcessMemoryFloat(processHandle, Constants.off_turnFactor, 0.00009999999747f, true);

                    // restore verts for body parts
                    if (bodyPartsHidden)
                    {
                        foreach (int offset in bodyPartVertOffsets.Keys)
                        {
                            Memory.WriteProcessMemoryFloat(processHandle, offset, bodyPartVertOffsets[offset]); // restore original value
                        }
                        bodyPartsHidden = false;
                    }
                }
                else
                {

                    // set opcodes to ret (0xC3)
                    buffer = new byte[] { 0xC3 };
                    Memory.WriteProcessMemory(processHandle, off_DNM_p_stDynamicsCameraMechanics, buffer, buffer.Length, ref bytesReadOrWritten);
                    Memory.WriteProcessMemory(processHandle, off_ForceCameraPos, buffer, buffer.Length, ref bytesReadOrWritten);
                    Memory.WriteProcessMemory(processHandle, off_ForceCameraTgt, buffer, buffer.Length, ref bytesReadOrWritten);

                    // lower turn speed
                    float sensitivityFactor = 0.5f;
                    sensitivityFactor = w.fpsModeSensitivity;
                    Memory.WriteProcessMemoryFloat(processHandle, Constants.off_turnFactor, 0.00009999999747f * sensitivityFactor, true);

                    // set verts for body parts to 0 to hide them
                    if (!bodyPartsHidden)
                    {
                        foreach (int offset in bodyPartVertOffsets.Keys)
                        {
                            Memory.WriteProcessMemoryFloat(processHandle, offset, 0); // hide
                        }
                        bodyPartsHidden = true;
                    }
                }

                if (!tempDisable && !frozen)
                {

                    // Add speed to the matrix
                    matrix.m.M14 += raymanSpeed.X / 100.0f;
                    matrix.m.M24 += raymanSpeed.Y / 100.0f;
                    matrix.m.M34 += raymanSpeed.Z / 100.0f;

                    matrix.Write(processHandle, off_cameraMatrix);

                    Memory.WriteProcessMemoryFloat(processHandle, off_cameraTargetX, matrix.m.M14);
                    Memory.WriteProcessMemoryFloat(processHandle, off_cameraTargetY, matrix.m.M24);
                    Memory.WriteProcessMemoryFloat(processHandle, off_cameraTargetZ, matrix.m.M34);

                    Memory.WriteProcessMemoryFloat(processHandle, off_cameraFOV, 1.8f);
                }
                else
                {
                    Memory.WriteProcessMemoryFloat(processHandle, off_cameraFOV, 1.2f);
                }

                Thread.Sleep(15);
            }

            buffer = new byte[] { 0x81 };
            Memory.WriteProcessMemory(processHandle, off_DNM_p_stDynamicsCameraMechanics, buffer, buffer.Length, ref bytesReadOrWritten);
            buffer = new byte[] { 0x53 };
            Memory.WriteProcessMemory(processHandle, off_ForceCameraPos, buffer, buffer.Length, ref bytesReadOrWritten);
            buffer = new byte[] { 0x83 };
            Memory.WriteProcessMemory(processHandle, off_ForceCameraTgt, buffer, buffer.Length, ref bytesReadOrWritten);

            // restore verts for body parts
            foreach (int offset in bodyPartVertOffsets.Keys)
            {
                Memory.WriteProcessMemoryFloat(processHandle, offset, bodyPartVertOffsets[offset]); // restore original value
            }

            // restore fov
            Memory.WriteProcessMemoryFloat(processHandle, off_cameraFOV, 1.2f);
        }
    }
}
