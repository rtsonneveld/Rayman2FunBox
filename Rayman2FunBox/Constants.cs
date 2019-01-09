using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rayman2FunBox
{
    static class Constants
    {
        public const int off_engineStructure = 0x500380;
        public const int off_engineMode = off_engineStructure + 0x0;
        public const int off_levelName = off_engineStructure + 0x1F;
        public const int off_healthpointer_1 = 0x500584;
        public const int off_voidpointer = 0x4B9BC8;
        public const int off_brightnesspointer = 0x4A0488;
        public const int off_cameraArrayPointer = 0x500550;
        public const int off_mainChar = 0x500578;
        public const int off_turnFactor = 0x49CC3C;

        public const int off_inputX = 0x4B9BA0;
        public const int off_inputY = 0x4B9BA4;

        public const int off_objectTypes = 0x005013E0;
    }
}
