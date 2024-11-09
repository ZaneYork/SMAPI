using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using NUnit.Framework;
using StardewModdingAPI.Framework.Models;

namespace SMAPI.Tests.Core;

/// <summary>Unit tests which validate assumptions about .NET used in the SMAPI implementation.</summary>
[TestFixture]
internal class AssumptionTests
{
    /*********
    ** Unit tests
    *********/
    /****
    ** Constructor
    ****/
    [Test(Description = $"Assert that {nameof(HashSet<string>)} maintains insertion order when no elements are removed. If this fails, we'll need to change the implementation for the {nameof(SConfig.ModsToLoadEarly)} and {nameof(SConfig.ModsToLoadLate)} options.")]
    [TestCase("construct from array")]
    [TestCase("add incrementally")]
    public void HashSet_MaintainsInsertionOrderWhenNoElementsAreRemoved(string populateMethod)
    {
        // arrange
        string[] inserted = Enumerable.Range(0, 1000)
            .Select(_ => Guid.NewGuid().ToString("N"))
            .ToArray();

        // act
        HashSet<string> set;
        switch (populateMethod)
        {
            case "construct from array":
                set = new(inserted, StringComparer.OrdinalIgnoreCase);
                break;

            case "add incrementally":
                set = new(StringComparer.OrdinalIgnoreCase);
                foreach (string value in inserted)
                    set.Add(value);
                break;

            default:
                throw new AssertionFailedException($"Unknown populate method '{populateMethod}'.");
        }

        // assert
        string[] actualOrder = set.ToArray();
        actualOrder.Should().HaveCount(inserted.Length);
        for (int i = 0; i < inserted.Length; i++)
        {
            string expected = inserted[i];
            string actual = actualOrder[i];

            if (actual != expected)
                throw new AssertionFailedException($"The hash set differed at index {i}: expected {expected}, but found {actual} instead.");
        }
    }
}
