﻿using PInvoke;
using System;
using System.Collections.Generic;
using System.Text;

namespace WindowArranger
{
    public static class RECTExtensions
    {
        public static int OriginX(this in RECT self)
        {
            return self.left;
        }

        public static int OriginY(this in RECT self)
        {
            return self.top;
        }

        public static int Height(this in RECT self)
        {
            return self.bottom - self.top;
        }

        public static int Width(this in RECT self)
        {
            return self.right - self.left;
        }

        public static string Summary(this in RECT self)
        {
            return $"{self.Width()} x {self.Height()} @ ({self.OriginX()}, {self.OriginY()})";
        }

    }
}
