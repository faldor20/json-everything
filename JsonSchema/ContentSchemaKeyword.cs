﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Json.Schema;

/// <summary>
/// Handles `contentSchema`.
/// </summary>
[SchemaPriority(20)]
[SchemaKeyword(Name)]
[SchemaSpecVersion(SpecVersion.Draft201909)]
[SchemaSpecVersion(SpecVersion.Draft202012)]
[SchemaSpecVersion(SpecVersion.DraftNext)]
[Vocabulary(Vocabularies.Content201909Id)]
[Vocabulary(Vocabularies.Content202012Id)]
[Vocabulary(Vocabularies.ContentNextId)]
[JsonConverter(typeof(ContentSchemaKeywordJsonConverter))]
public class ContentSchemaKeyword : IJsonSchemaKeyword, ISchemaContainer, IEquatable<ContentSchemaKeyword>
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "contentSchema";

	/// <summary>
	/// The schema against which to evaluate the content.
	/// </summary>
	public JsonSchema Schema { get; }

	/// <summary>
	/// Creates a new <see cref="ContentSchemaKeyword"/>.
	/// </summary>
	/// <param name="value">The schema against which to evaluate the content.</param>
	public ContentSchemaKeyword(JsonSchema value)
	{
		Schema = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Performs evaluation for the keyword.
	/// </summary>
	/// <param name="context">Contextual details for the evaluation process.</param>
	public void Evaluate(EvaluationContext context)
	{
		context.EnterKeyword(Name);
		var schemaValueType = context.LocalInstance.GetSchemaValueType();
		if (schemaValueType != SchemaValueType.String)
		{
			context.WrongValueKind(schemaValueType);
			return;
		}

		context.LocalResult.SetAnnotation(Name, JsonSerializer.SerializeToNode(Schema));
		context.ExitKeyword(Name, context.LocalResult.IsValid);
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(ContentSchemaKeyword? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return Equals(Schema, other.Schema);
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object obj)
	{
		return Equals(obj as ContentSchemaKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return Schema.GetHashCode();
	}
}

internal class ContentSchemaKeywordJsonConverter : JsonConverter<ContentSchemaKeyword>
{
	public override ContentSchemaKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var schema = JsonSerializer.Deserialize<JsonSchema>(ref reader, options)!;

		return new ContentSchemaKeyword(schema);
	}
	public override void Write(Utf8JsonWriter writer, ContentSchemaKeyword value, JsonSerializerOptions options)
	{
		writer.WritePropertyName(ContentSchemaKeyword.Name);
		JsonSerializer.Serialize(writer, value.Schema, options);
	}
}