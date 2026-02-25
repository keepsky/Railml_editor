using System;
using System.Windows;
using RailmlEditor.ViewModels;
using RailmlEditor.ViewModels.Elements;

namespace RailmlEditor.Services
{
    /// <summary>
    /// (리팩터링 2단계 핵심) 새로운 RailML 요소를 생성하는 역할을 전담하는 '팩토리(공장)' 클래스입니다.
    /// 예전에는 MainViewModel 안에 스위치문이 길게 늘어져 있었지만, 이제는 요소 생성이 필요할 때 이 공장에 주문만 넣으면 됩니다.
    /// 이렇게 생성 로직을 분리해두면, 나중에 새로운 종류의 요소(예: 역사, 승강장 등)가 추가되어도 이 파일만 수정하면 되므로 관리가 편해집니다.
    /// </summary>
    public static class ElementFactoryService
    {
        /// <summary>
        /// 요청받은 타입 문자열(예: "Track", "Switch")에 알맞은 뷰모델 객체를 생성해서 돌려줍니다.
        /// </summary>
        /// <param name="type">생성할 객체의 종류 (문자열)</param>
        /// <param name="position">객체가 처음 생성될 화면 상의 위치(마우스 클릭 좌표 등)</param>
        /// <param name="idGenerator">새로운 ID를 발급해주는 함수 (예: "tr1" 등 자동 부여)</param>
        /// <returns>생성된 구체적인 BaseElementViewModel 객체 (올바르지 않은 타입이면 null 반환)</returns>
        public static BaseElementViewModel? CreateElement(string type, Point position, Func<string, string> idGenerator)
        {
            switch (type)
            {
                case "Track":
                    return new TrackViewModel
                    {
                        Id = idGenerator("tr"),
                        X = position.X,
                        Y = position.Y,
                        Length = 100
                    };
                case "Switch":
                    return new SwitchViewModel
                    {
                        Id = idGenerator("sw"),
                        X = position.X,
                        Y = position.Y
                    };
                case "Signal":
                    return new SignalViewModel
                    {
                        Id = idGenerator("sig"),
                        X = position.X,
                        Y = position.Y
                    };
                case "Corner":
                    double mx = position.X + 20;
                    double my = position.Y - 40;
                    return new CurvedTrackViewModel
                    {
                        Id = idGenerator("tr"),
                        Code = "corner",
                        X = position.X,
                        Y = position.Y,
                        MX = mx,
                        MY = my,
                        X2 = mx + 10,
                        Y2 = my
                    };
                case "Route":
                    return new RouteViewModel
                    {
                        Id = idGenerator("R")
                    };
                default:
                    return null;
            }
        }
    }
}
