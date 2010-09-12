using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Raven.Client.Document;
using Raven.Database.Indexing;

namespace Raven.Client.Indexes
{
	/// <summary>
	/// This class attempts to provide a strongly typed index defintion on the client.
	/// It is here solely as a convienance, and it is _expected_ to fail in some scenarios.
	/// The recommended way is to define indexes outside your code, using the Web UI.
	/// </summary>
	public class IndexDefinition<TDocument, TReduceResult> 
	{
		public Expression<Func<IEnumerable<TDocument>, IEnumerable>> Map { get; set; }
		public Expression<Func<IEnumerable<TReduceResult>, IEnumerable>> Reduce { get; set; }

		public IDictionary<Expression<Func<TReduceResult, object>>, FieldStorage> Stores { get; set; }
		public IDictionary<Expression<Func<TReduceResult, object>>, FieldIndexing> Indexes { get; set; }
		public IDictionary<Expression<Func<TReduceResult, object>>, SortOptions> SortOptions { get; set; }

		public IndexDefinition()
		{
			Stores = new Dictionary<Expression<Func<TReduceResult, object>>, FieldStorage>();
			Indexes = new Dictionary<Expression<Func<TReduceResult, object>>, FieldIndexing>();
			SortOptions = new Dictionary<Expression<Func<TReduceResult, object>>, SortOptions>();
		}

		public IndexDefinition ToIndexDefinition(DocumentConvention convention)
		{
			return new IndexDefinition
			{
				Map = PruneToFailureLinqQueryAsStringToWorkableCode(Map, convention, "docs." + convention.GetTypeTagName(typeof(TDocument))),
				Reduce = PruneToFailureLinqQueryAsStringToWorkableCode(Reduce, convention, "results"),
				Indexes = ConvertToStringDictionary(Indexes),
				Stores = ConvertToStringDictionary(Stores),
				SortOptions = ConvertToStringDictionary(SortOptions)
			};
		}

		private static IDictionary<string, TValue> ConvertToStringDictionary<TValue>(IEnumerable<KeyValuePair<Expression<Func<TReduceResult, object>>, TValue>> input)
		{
			var result = new Dictionary<string, TValue>();
			foreach (var value in input)
			{
				result[(GetMemberExpression(value.Key)).Member.Name] = value.Value;
			}
			return result;
		}

		private static MemberExpression GetMemberExpression(Expression<Func<TReduceResult, object>> value)
		{
			if(value.Body is UnaryExpression)
				return (MemberExpression)((UnaryExpression) value.Body).Operand;
			return (MemberExpression) value.Body;
		}


		private static string PruneToFailureLinqQueryAsStringToWorkableCode<T>(
			Expression<Func<IEnumerable<T>, IEnumerable>> expr, 
			DocumentConvention convention,
			string querySource)
		{
			if (expr == null)
				return null;
			var expression = expr.Body;

			switch (expression.NodeType)
			{
				case ExpressionType.ConvertChecked:
				case ExpressionType.Convert:
					expression = ((UnaryExpression) expression).Operand;
					break;
			}

#if !NET_3_5
			var linqQuery = ExpressionStringBuilder.ExpressionToString(convention, expression);
#else
            var linqQuery =expression.ToString();
#endif

			linqQuery = querySource + linqQuery.Substring(expr.Parameters[0].Name.Length);

			linqQuery = ReplaceAnonymousTypeBraces(linqQuery);
			linqQuery = Regex.Replace(linqQuery, @"new ((VB\$)|(<>))[\w_]+`\d+", "new ");// remove anonymous types
			linqQuery = Regex.Replace(linqQuery, @"<>([a-z])_", "__$1_"); // replace <>h_ in transperant identifiers
			const string pattern = @"(\.Where\(|\.Select\(|\.GroupBy\(|\.SelectMany)";
			linqQuery = Regex.Replace(linqQuery, pattern, "\r\n\t$1"); // formatting
			return linqQuery;
		}

		private static string ReplaceAnonymousTypeBraces(string linqQuery)
		{
			const string pattern = @"new ((VB\$)|(<>))[\w_]+`\d+";
			var matches = Regex.Matches(linqQuery, pattern);
			for (int i = 0; i < matches.Count; i++ )
			{
				var match = matches[i];
				int endBrace = -1;
				var startBrace = linqQuery[match.Index + match.Length];
				int startIndex = match.Index + match.Length;
				if (startBrace != '(')
					break;

				int otherBraces = 0;
				for (int j = startIndex+1; j < linqQuery.Length; j++)
				{
                    if (linqQuery[j] == '(')
                    {
                        otherBraces++;
                        continue;
                    }
                    else if (linqQuery[j] != ')')
                        continue;
					if (otherBraces == 0)
					{
						endBrace = j;
						break;
					}
					otherBraces--;
				}
				if (endBrace != -1)
				{
					string s = linqQuery.Substring(0, match.Index + match.Length) + "{";
					s += linqQuery.Substring(startIndex + 1, endBrace - startIndex - 1) + "}";
					s += linqQuery.Substring(endBrace + 1);
					linqQuery = s;
					matches = Regex.Matches(linqQuery, pattern);
					continue;
				}
				break;
			}
			return linqQuery;
		}
	}

	public class IndexDefinition<TDocument> : IndexDefinition<TDocument, object> { }
}