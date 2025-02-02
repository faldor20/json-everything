﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.More;

namespace Json.Schema;

/// <summary>
/// Handles `examples`.
/// </summary>
[SchemaKeyword(Name)]
[SchemaSpecVersion(SpecVersion.Draft6)]
[SchemaSpecVersion(SpecVersion.Draft7)]
[SchemaSpecVersion(SpecVersion.Draft201909)]
[SchemaSpecVersion(SpecVersion.Draft202012)]
[SchemaSpecVersion(SpecVersion.DraftNext)]
[Vocabulary(Vocabularies.Metadata201909Id)]
[Vocabulary(Vocabularies.Metadata202012Id)]
[Vocabulary(Vocabularies.MetadataNextId)]
[JsonConverter(typeof(ExamplesKeywordJsonConverter))]
public class ExamplesKeyword : IJsonSchemaKeyword, IEquatable<ExamplesKeyword>
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "examples";

	/// <summary>
	/// The collection of example values.
	/// </summary>
	public IReadOnlyList<JsonNode?> Values { get; }

	/// <summary>
	/// Creates a new <see cref="ExamplesKeyword"/>.
	/// </summary>
	/// <param name="values">The collection of example values.</param>
	public ExamplesKeyword(params JsonNode?[] values)
	{
		Values = values.ToReadOnlyList() ?? throw new ArgumentNullException(nameof(values));
	}

	/// <summary>
	/// Creates a new <see cref="ExamplesKeyword"/>.
	/// </summary>
	/// <param name="values">The collection of example values.</param>
	public ExamplesKeyword(IEnumerable<JsonNode?> values)
	{
		Values = values.ToReadOnlyList() ?? throw new ArgumentNullException(nameof(values));
	}

	/// <summary>
	/// Performs evaluation for the keyword.
	/// </summary>
	/// <param name="context">Contextual details for the evaluation process.</param>
	public void Evaluate(EvaluationContext context)
	{
		context.EnterKeyword(Name);
		context.LocalResult.SetAnnotation(Name, Values.ToJsonArray());
		context.ExitKeyword(Name, true);
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(ExamplesKeyword? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return Values.ContentsEqual(other.Values, JsonNodeEqualityComparer.Instance);
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object obj)
	{
		return Equals(obj as ExamplesKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return Values.GetUnorderedCollectionHashCode(element => element?.GetEquivalenceHashCode() ?? 0);
	}
}

internal class ExamplesKeywordJsonConverter : JsonConverter<ExamplesKeyword>
{
	public override ExamplesKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var array = JsonSerializer.Deserialize<JsonArray>(ref reader);
		if (array is null)
			throw new JsonException("Expected an array, but received null");

		return new ExamplesKeyword((IEnumerable<JsonNode>)array!);
	}
	public override void Write(Utf8JsonWriter writer, ExamplesKeyword value, JsonSerializerOptions options)
	{
		writer.WritePropertyName(ExamplesKeyword.Name);
		writer.WriteStartArray();
		foreach (var node in value.Values)
		{
			JsonSerializer.Serialize(writer, node, options);
		}
		writer.WriteEndArray();
	}
}