ENTERABILITY_YES  = 0
ENTERABILITY_NO   = 1
ENTERABILITY_SOON = 2

function Clamp01(value)
	if (value > 1) then
		return 1
	elseif (value < 0) then
		return 0
	end

	return value
end

function OnUpdate_GasGenerator(furniture, deltaTime)
	if(furniture.tile.room == nil) then
		return "Furniture's room was null."
	end

	if (furniture.tile.room.GetGasAmount("O2") < 0.20) then
		furniture.tile.room.ChangeGas("O2", 0.01 * deltaTime)
	else
		-- what to do?
	end
	
	return
end

function OnUpdate_Door(furniture, deltaTime)
	if(furniture.GetParameter("is_opening") >= 1.0) then
		furniture.ChangeParameter("openness", deltaTime * 4) -- FIXME: maybe a parameter?
		if (furniture.GetParameter("openness") >= 1) then
			furniture.SetParameter("is_opening", 0)
		end
	else
		furniture.ChangeParameter("openness", deltaTime * -4)
	end

	furniture.SetParameter("openness", Clamp01(furniture.GetParameter("openness")))

	if (furniture.cbOnChanged != nil) then
		furniture.cbOnChanged(furniture)
	end
end

function IsEnterable_Door(furniture)
	furniture.SetParameter("is_opening", 1)
	
	if (furniture.GetParameter("openness") >= 1) then
	   return ENTERABILITY_YES --ENTERABILITY.Yes
	end
	
	return ENTERABILITY_SOON --ENTERABILITY.Soon
end

return "LUA Script Parsed!"