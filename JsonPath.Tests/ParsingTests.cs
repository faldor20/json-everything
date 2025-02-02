﻿using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Json.Path.Tests;

public class ParsingTests
{
	public static IEnumerable<TestCaseData> SuccessCases =>
		new[]
		{
			new TestCaseData("$['foo']"),
			new TestCaseData("$[ 'foo']"),
			new TestCaseData("$['foo' ]"),

			new TestCaseData("$[1]"),
			new TestCaseData("$[ 1]"),
			new TestCaseData("$[1 ]"),
			new TestCaseData("$[42]"),
			new TestCaseData("$[-1]"),
			new TestCaseData("$[-42]"),

			new TestCaseData("$[*]"),
			new TestCaseData("$[ *]"),
			new TestCaseData("$[* ]"),

			new TestCaseData("$[:]"),
			new TestCaseData("$[1:]"),
			new TestCaseData("$[1::]"),
			new TestCaseData("$[1:2]"),
			new TestCaseData("$[1:2:]"),
			new TestCaseData("$[1::3]"),
			new TestCaseData("$[1:2:3]"),
			new TestCaseData("$[-1:2:3]"),
			new TestCaseData("$[1:-2:3]"),
			new TestCaseData("$[1:2:-3]"),
			new TestCaseData("$[ 1:2:3]"),
			new TestCaseData("$[1 :2:3]"),
			new TestCaseData("$[1: 2:3]"),
			new TestCaseData("$[1:2 :3]"),
			new TestCaseData("$[1:2: 3]"),
			new TestCaseData("$[1:2:3 ]"),

			new TestCaseData("$.foo"),

			new TestCaseData("$.*"),

			new TestCaseData("$..foo"),
			new TestCaseData("$..*"),
			new TestCaseData("$..[1]"),
			new TestCaseData("$..[1,2]"),

			new TestCaseData("$[?@.foo]"),
			new TestCaseData("$[?(@.foo)]"),
			new TestCaseData("$[?(@.foo && @.bar)]"),
			new TestCaseData("$[?(!@.foo)]"),
			new TestCaseData("$[?(@.foo && !@.bar)]"),
			new TestCaseData("$[?!(@.foo == false)]"),
			new TestCaseData("$[?(@.foo == false)]"),
			new TestCaseData("$[?(@['name'] == null || @['name'] == 'abc')]"),

			new TestCaseData("$[1,'foo',1:2:3,*]"),
		};

	[TestCaseSource(nameof(SuccessCases))]
	public void ParseSuccess(string path)
	{
		Console.WriteLine(JsonPath.Parse(path));
	}

	public static IEnumerable<TestCaseData> OptionalCases =>
		new[]
		{
			new TestCaseData("$[?(@.foo==(4+5))]"),
			new TestCaseData("$[?(@.foo==2*(4+5))]"),
			new TestCaseData("$[?(@.foo==2+(4+5))]"),
			new TestCaseData("$[?(@.foo==2-(4+5))]"),
		};

	[TestCaseSource(nameof(OptionalCases))]
	public void ParseWithOptions(string path)
	{
		Console.WriteLine(JsonPath.Parse(path, new PathParsingOptions{AllowMathOperations = true}));
	}

	[TestCaseSource(nameof(OptionalCases))]
	public void ParseWithoutOptions(string path)
	{
		Assert.Throws<PathParseException>(() => JsonPath.Parse(path));
	}
}