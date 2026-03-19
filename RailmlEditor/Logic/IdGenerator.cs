using System;
using System.Collections.Generic;
using System.Linq;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;

namespace RailmlEditor.Logic
{
    /// <summary>
    /// 화면에 요소를 추가할 때, 기존에 있던 요소들의 ID 번호를 확인해서 
    /// 겹치지 않는 다음 번호(예: tr1, tr2가 있으면 "tr3")를 자동으로 만들어주는 유틸리티 클래스입니다.
    /// </summary>
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


