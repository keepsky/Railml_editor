using System;
using System.Collections.Generic;
using System.Linq;
using RailmlEditor.ViewModels;

namespace RailmlEditor.Logic
{
    public class IdGenerator
    {
        public string GetNextId(IEnumerable<BaseElementViewModel> elements, string prefix)
        {
            int max = 0;
            foreach (var el in elements)
            {
                if (!string.IsNullOrEmpty(el.Id) && el.Id.StartsWith(prefix))
                {
                    string numberPart = el.Id.Substring(prefix.Length);
                    if (int.TryParse(numberPart, out int num))
                    {
                        if (num > max) max = num;
                    }
                }
            }
            return $"{prefix}{max + 1}";
        }
    }
}
