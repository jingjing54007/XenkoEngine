﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using NUnit.Framework;

namespace SiliconStudio.Xenko.Particles.Tests
{
    class VisualTestChildren : GameTest
    {
        public VisualTestChildren() : base("VisualTestChildren")
        {
            IndividualTestVersion = 1;  //  Changes in particle spawning
            IndividualTestVersion += 4;  //  Changed to avoid collisions with 1.10
        }

        [Test]
        public void RunVisualTests()
        {
            RunGameTest(new VisualTestChildren());
        }
    }
}
