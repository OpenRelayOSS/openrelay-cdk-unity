//------------------------------------------------------------------------------
// <copyright file="Words.cs" company="FurtherSystem Co.,Ltd.">
// Copyright (C) 2018 FurtherSystem Co.,Ltd. All rights reserved.
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
// </copyright>
// <author>FurtherSystem Co.,Ltd.</author>
// <email>info@furthersystem.com</email>
// <summary>
// OpenRelay performance sample.
// </summary>
//------------------------------------------------------------------------------
using System.Text;
using UnityEngine;

namespace Com.FurtherSystems.OpenRelayPerformanceSample
{
    public class Words
    {
        private static readonly string[] Oceans = new string[] {
            "",
            "arctic",
            "atlantic",
            "indian",
            "pacific",
            "southern"
        };
        private static readonly string[] Grades = new string[] {
            "",
            "angel",
            "big",
            "small",
            "new",
            "old",
            "good",
            "bad",
            "hot",
            "cold",
            "hot",
            "difficult",
            "easy",
            "expensive",
            "cheep",
            "high",
            "low",
            "interesting",
            "delicious",
            "busy",
            "enjoyable",
            "white",
            "red",
            "blue",
            "power",
            "neon",
            "black",
            "gold",
            "silver",
            "sword",
            "cardinal",
            "marbled",
            "space",
        };
        private static readonly string[] Genuses = new string[] {
            "fish",
            "betta",
            "tetra",
            "guppy",
            "tail",
            "arowana",
            "shark",
            "cichlid",
            "barb",
            "danio",
            "loach",
            "penguin",
            "tortoise",
            "doggo",
            "gorilla",
            "mermaid",
            "ipupiara",
            "merrow",
            "siren",
            "seahog",
            "amabie",
            "coomara",
        };

        public static void Initialize(int seed)
        {
            Random.InitState(seed);
        }

        public static string GeneratePhrase()
        {
            var oceansIndex = Random.Range(0, Oceans.Length);
            var oceans = Oceans[oceansIndex];
            var gradesIndex = Random.Range(0, Grades.Length);
            var grades = Grades[gradesIndex];
            var genusesIndex = Random.Range(0, Genuses.Length);
            var genuses = Genuses[genusesIndex];
            return new StringBuilder().Append(oceans).Append(grades).Append(genuses).ToString();
        }
    }
}
