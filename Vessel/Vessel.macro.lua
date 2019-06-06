
--[[
    Vessel module macros

    * Common\AltF11 - smart file history instead of standard
    * Common\CtrlShiftF11 - show standard "File view history"
    * Shell Editor Viewer\AltF12 - smart folder history instead of standard
    * Shell\CtrlShiftF12 - show standard "Folder history"
]]

Macro {
  area="Common"; key="AltF11"; description="Vessel: Smart file history"; action=function()
    if Plugin.Menu("10435532-9BB3-487B-A045-B0E6ECAAB6BC", "58AD5E13-D2BA-4F4C-82CD-F53A66E9E8C0") then
        Keys "1"
    else
        Keys "AKey"
    end
  end;
}

Macro {
  area="Common"; key="CtrlShiftF11"; description="Vessel: Standard file history"; action=function()
  Keys "AltF11"
  end;
}

Macro {
  area="Shell Editor Viewer"; key="AltF12"; description="Vessel: Smart folder history"; action=function()
    if Plugin.Menu("10435532-9BB3-487B-A045-B0E6ECAAB6BC", "58AD5E13-D2BA-4F4C-82CD-F53A66E9E8C0") then
        Keys "2"
    else
        Keys "AKey"
    end
  end;
}

Macro {
  area="Shell"; key="CtrlShiftF12"; description="Vessel: Standard folder history"; action=function()
    Keys "AltF12"
  end;
}
