﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Pointer;

namespace Json.Schema;

/// <summary>
/// Handles `$ref`.
/// </summary>
[SchemaKeyword(Name)]
[SchemaSpecVersion(SpecVersion.Draft6)]
[SchemaSpecVersion(SpecVersion.Draft7)]
[SchemaSpecVersion(SpecVersion.Draft201909)]
[SchemaSpecVersion(SpecVersion.Draft202012)]
[SchemaSpecVersion(SpecVersion.DraftNext)]
[Vocabulary(Vocabularies.Core201909Id)]
[Vocabulary(Vocabularies.Core202012Id)]
[Vocabulary(Vocabularies.CoreNextId)]
[JsonConverter(typeof(RefKeywordJsonConverter))]
public class RefKeyword : IJsonSchemaKeyword, IEquatable<RefKeyword>
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "$ref";

	/// <summary>
	/// The URI reference.
	/// </summary>
	public Uri Reference { get; }

	/// <summary>
	/// Creates a new <see cref="RefKeyword"/>.
	/// </summary>
	/// <param name="value">The URI reference.</param>
	public RefKeyword(Uri value)
	{
		Reference = value;
	}

	/// <summary>
	/// Performs evaluation for the keyword.
	/// </summary>
	/// <param name="context">Contextual details for the evaluation process.</param>
	public void Evaluate(EvaluationContext context)
	{
		context.EnterKeyword(Name);

		Uri newUri;
		string fragment;
		// If the uri is a file we need to set the fragment manually because it will be lost in the uri
		if (context.Scope.LocalScope.IsFile && Reference.OriginalString.Contains("#"))
		{
			if (Reference.OriginalString.StartsWith("#"))
			{
				fragment = Reference.OriginalString;
				newUri = context.Scope.LocalScope;
			}

			else
			{
				var parts=Reference.OriginalString.Split('#');
				if (parts.Length != 2)
					throw new JsonSchemaException(
						$"Given a reference with more than one '#' in it. We cannot tell if this is a fragment, or where the fragment starts. Please don't use '#'s in fileNames or paths.\n Reference:{Reference.OriginalString} ");
				fragment = '#'+parts[1];
				newUri =new Uri(context.Scope.LocalScope, parts[0]);

			}
		}
		else
		{
			newUri = new Uri(context.Scope.LocalScope, Reference);
			fragment = newUri.Fragment;
		}

		var navigation = (newUri.OriginalString, context.InstanceLocation);
		if (context.NavigatedReferences.Contains(navigation))
			throw new JsonSchemaException($"Encountered circular reference at schema location `{newUri}` and instance location `{context.InstanceLocation}`");

		var newBaseUri = new Uri(newUri.GetLeftPart(UriPartial.Query));

		JsonSchema? targetSchema = null;
		var targetBase = context.Options.SchemaRegistry.Get(newBaseUri) ??
		                 throw new JsonSchemaException($"Cannot resolve base schema from `{newUri}`");

		if (JsonPointer.TryParse(fragment, out var pointerFragment))
		{
			if (targetBase == null)
				throw new JsonSchemaException($"Cannot resolve base schema from `{newUri}`");

			targetSchema = targetBase.FindSubschema(pointerFragment!, context.Options);
		}
		else
		{
			var anchorFragment = fragment.Substring(1);
			if (!AnchorKeyword.AnchorPattern.IsMatch(anchorFragment))
				throw new JsonSchemaException($"Unrecognized fragment type `{newUri}`");

			if (targetBase is JsonSchema targetBaseSchema &&
			    targetBaseSchema.Anchors.TryGetValue(anchorFragment, out var anchorDefinition))
				targetSchema = anchorDefinition.Schema;
		}

		if (targetSchema == null)
			throw new JsonSchemaException($"Cannot resolve schema `{newUri}`");

		context.NavigatedReferences.Add(navigation);
		context.Push(context.EvaluationPath.Combine(Name), targetSchema);
		if (pointerFragment != null)
			context.LocalResult.SetSchemaReference(pointerFragment);
		context.Evaluate();
		var result = context.LocalResult.IsValid;
		context.Pop();
		context.NavigatedReferences.Remove(navigation);
		if (!result)
			context.LocalResult.Fail();

		context.ExitKeyword(Name, context.LocalResult.IsValid);
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(RefKeyword? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return Equals(Reference, other.Reference);
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object obj)
	{
		return Equals(obj as RefKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return Reference.GetHashCode();
	}
}

internal class RefKeywordJsonConverter : JsonConverter<RefKeyword>
{
	public override RefKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var uri = reader.GetString();
		return new RefKeyword(new Uri(uri!, UriKind.RelativeOrAbsolute));


	}
	public override void Write(Utf8JsonWriter writer, RefKeyword value, JsonSerializerOptions options)
	{
		writer.WritePropertyName(RefKeyword.Name);
		JsonSerializer.Serialize(writer, value.Reference, options);
	}
}