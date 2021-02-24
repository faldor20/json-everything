﻿ using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Json.Schema.Generation.Intents;

namespace Json.Schema.Generation
{
	/// <summary>
	/// Provides meta-data about the generation process.
	/// </summary>
	public class SchemaGeneratorContext
	{
		private class ContextCount
		{
			public SchemaGeneratorContext Context { get; }
			public int Count { get; set; }

			public ContextCount(SchemaGeneratorContext context)
			{
				Context = context;
				Count = 1;
			}
		}

		/// <summary>
		/// The CLR type currently being processed.
		/// </summary>
		public Type Type { get; }
		/// <summary>
		/// The set of attributes.  Will be populated when an attribute has a property.
		/// </summary>
		public List<Attribute> Attributes { get; }
		/// <summary>
		/// The current set of keyword intents.
		/// </summary>
		public List<ISchemaKeywordIntent> Intents { get; } = new List<ISchemaKeywordIntent>();

		internal SchemaGeneratorContext(Type type, List<Attribute> attributes)
		{
			Type = type;
			Attributes = attributes;
		}

		internal void GenerateIntents()
		{
			var generator = GeneratorRegistry.Get(Type);
			generator?.AddConstraints(this);

			AttributeHandler.HandleAttributes(this);
		}

		internal void Optimize()
		{
			var thisHash = GetHashCode();
			var allContexts = GetChildContexts();
			var defsByHashCode = allContexts.Where(g => g.Value.Count > 1)
				.ToDictionary(g => g.Key, g => g.Value.Context);

			var currentNames = new List<string>();
			var defs = new Dictionary<string, SchemaGeneratorContext>();
			var contextContainers = Intents.OfType<IContextContainer>().ToList();
			foreach (var def in defsByHashCode)
			{
				var name = def.Value.GetDefName(currentNames);
				var refIntent = new RefIntent(new Uri(def.Key == thisHash ? "#" : $"#/$defs/{name}", UriKind.Relative));
				var refContext = new SchemaGeneratorContext(def.Value.Type, null!);
				refContext.Intents.Add(refIntent);
				foreach (var intent in contextContainers)
				{
					intent.Replace(def.Key, refContext);
				}
				if (def.Key == thisHash) continue;
				defs[name] = def.Value;
			}

			if (defs.Any())
			{
				var defsIntent = new DefsIntent(defs);
				Intents.Add(defsIntent);
			}
		}

		private string GetDefName(List<string> currentNames)
		{
			var name = GetName(Type);
			var regex = new Regex($@"^{name}\d*$");
			var count = currentNames.Count(n => regex.IsMatch(n));
			if (count != 0)
				name += count;

			currentNames.Add(name);
			return name;
		}

		private static string GetName(Type type)
		{
			if (type.IsInteger()) return "integer";
			if (type == typeof(string)) return "string";
			if (type.IsArray()) return "array";
			if (type == typeof(bool)) return "boolean";
			return type.Name;
		}

		private Dictionary<int, ContextCount> GetChildContexts()
		{
			var contextsToCheck = new List<SchemaGeneratorContext>{this};
			var contextsReceived = new Dictionary<int, ContextCount>();
			while (contextsToCheck.Any())
			{
				var context = contextsToCheck[0];
				contextsToCheck.RemoveAt(0);

				var hash = context.GetHashCode();
				if (contextsReceived.TryGetValue(hash, out var count))
				{
					count.Count++;
					continue;
				}

				contextsToCheck.AddRange(context.Intents.OfType<IContextContainer>().SelectMany(i => i.GetContexts()));
				contextsReceived[hash] = new ContextCount(context);
			}

			return contextsReceived;
		}

		/// <summary>
		/// Applies the keyword to the <see cref="JsonSchemaBuilder"/>.
		/// </summary>
		/// <param name="builder">The schema builder.</param>
		/// <returns>The schema builder (for fluent syntax support).</returns>
		public JsonSchemaBuilder Apply(JsonSchemaBuilder? builder = null)
		{
			builder ??= new JsonSchemaBuilder();

			foreach (var intent in Intents)
			{
				intent.Apply(builder);
			}

			return builder;
		}

		/// <summary>Determines whether the specified object is equal to the current object.</summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;

			var other = (SchemaGeneratorContext) obj;
			return Type == other.Type &&
				   Intents.ContentsEqual(other.Intents);
		}

		/// <summary>Serves as the default hash function.</summary>
		/// <returns>A hash code for the current object.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Type.GetHashCode();
				hashCode = (hashCode * 397) ^ (Attributes?.GetAttributeSetHashCode() ?? 0);
				return hashCode;
			}
		}
	}
}