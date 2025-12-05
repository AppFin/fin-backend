using Fin.Domain.Global.Decorators;

namespace Fin.Domain.Menus.Enums;

public enum MenuPosition
{
    [FrontTranslateKey("finCore.features.menus.hide")]
    Hide = 0,
    [FrontTranslateKey("finCore.features.menus.leftTop")]
    LeftTop = 1,
    [FrontTranslateKey("finCore.features.menus.leftBottom")]
    LeftBottom = 2
}