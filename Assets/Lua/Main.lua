local function get_lib_from_name_path(name_path)
	local path = name_path
	local delimiter = name_path:find('<')
	if delimiter ~= nil then
		path = name_path:sub(1, delimiter - 1)
	end
	local names = path:split('.')
	local count = #names;
	local t = _G
	for index = 1, count - 1 do
		t = t[names[index]]
	end
	local name = names[count]
	if delimiter ~= nil then
		name = name .. name_path:sub(delimiter)
	end
	return t[name]
end

local raw_require = require
local function prequire(modname)
    local status, lib = pcall(raw_require, modname)
    if status then
        return lib
    end

	local status = using(modname)
	if status then
		local lib = get_lib_from_name_path(modname)
		if lib ~= nil then
			package.loaded[modname] = lib
		else
			package.loaded[modname] = true
		end
		return lib
	end

    print(debug.traceback(lib))
    return nil
end
require = prequire

--主入口函数。从这里开始lua逻辑
function Main()
	print("logic start")
end

--场景切换通知
function OnLevelWasLoaded(level)
	collectgarbage("collect")
	Time.timeSinceLevelLoad = 0
end

function OnApplicationQuit()
end