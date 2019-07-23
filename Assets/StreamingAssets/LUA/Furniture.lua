-- TODO: Figure out the nicest way to have unified defines/enums
-- between C# and Lua to remove duplication
ENTERABILITY_YES  = 0
ENTERABILITY_NO   = 1
ENTERABILITY_SOON = 2

---------------------- UTILITY --------------------
function Clamp01(value)
	if (value > 1) then
		return 1
	elseif (value < 0) then
		return 0
	end

	return value
end

---------------------- Furniture Actions --------------------
function OnUpdate_GasGenerator(furniture, deltaTime)
	if(furniture.tile.room == nil) then
		return "Furniture's room was null."
	end

	if (furniture.tile.room.GetGasPressure("O2") < 0.20) then
		furniture.tile.room.ChangeGas("O2", 0.1 * deltaTime)
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

function Stockpile_GetItemsFromFilter()
	-- // TODO: this should be reading in from some kind of UI

	-- Probably doesn't belong in Lua and should be calling a C# function

	-- // Since jobs copy arrays automatically, we could already have
	-- // an Inventory[] prepared and just return that (as sort of example filter)
	return { Inventory.__new("Steel Plate", 50, 0) }
end

function Stockpile_UpdateAction(furniture, deltaTime)
	-- Ensure that we have a job on the queue asking for either:
    -- (if we are empty): that any loose inventory be brought to us
    -- if we have something): then if we are still below the max stack size,
    -- that more of the same should be brought to us

    -- TODO: this function doesn't need to run each update. Once we get a lot of
    -- furniture in a running game, this will run a LOT more than required
    -- Instead, it only read needs to run whenever: 
    -- -- it gets created
    -- -- a good gets delivered (reset job)
    -- -- a good gets picked up (reset job)
	-- -- the UI's filter of allowed items gets changed
	
	if (furniture.tile.inventory != nil and furniture.tile.inventory.stackSize >= furniture.tile.inventory.maxStackSize) then
		-- We are full!
		furniture.CancelJobs()
		return
	end

	-- Maybe we already have a job queued up?
    if (furniture.JobCount() > 0) then
     	-- All done
		 return
	end

	-- // We are currently not full but don't have a job either
	-- // Two possibilities: either we have some inventory or we have no inventory

	-- // Third possibility: Something is whack
	if (furniture.tile.inventory != nil and furniture.tile.inventory.stackSize == 0) then
		furniture.CancelJobs()
		return "Stockpile has a zero-size stack. This is clearling WRONG!"
	end

-- // TODO: in the future, stockpiles, rather than being a bunch of individual
-- // 1x1 tiles, should manifest themselves as a single, large object
-- // this would represent our first and probably only variable sized furniture
-- // What happens if there's a hole in the stockpile (if an actual furniture is
-- // installed in the middle?)
-- // In any case, once we implement 'mega stockpiles', then the job-creation system
-- // can be smarter in that even if the stockpile has stuff in it, it can
-- // also still be requesting different object types in its job creation

	itemsDesired = {}

	if (furniture.tile.inventory == nil) then
		--Debug.Log("Creating job for new stack.")
		itemsDesired = Stockpile_GetItemsFromFilter()
	else
		--Debug.Log("Creating job for existing stack.")
		desInv = furniture.tile.inventory.Clone()
		desInv.maxStackSize = desInv.maxStackSize - desInv.stackSize
		desInv.stackSize = 0

		itemsDesired = { desInv }
	end

	j = Job.__new(
		furniture.tile,
		nil,
		nil,
		0,
		itemsDesired,
		false
	)

	-- // TODO: add stockpile priorities so we can take from lower to higher
	j.canTakeFromStockpile = false

	j.RegisterJobWorkedCallback("Stockpile_JobWorked")
	furniture.AddJob(j)
end

function Stockpile_JobWorked(j)
	j.CancelJob()

	-- // TODO: change this when we figure out what to do for all/any pickup job
	--values = j.GetInventoryRequirementValues()
	for k, inv in pairs(j.inventoryRequirements) do
		if (inv.stackSize > 0) then
			World.Current.inventoryManager.PlaceInventory(j.tile, inv)
			return  --There should be no way we ever end up with more than one inventory req with stackSize > 0
		end
	end
end

function MiningDroneStation_UpdateAction(furniture, deltaTime)
	spawnSpot = furniture.GetSpawnSpotTile()

	if (furniture.JobCount() > 0) then
		-- Check to see if the Metal Plate destination tile is full
		if (spawnSpot.inventory != nil and spawnSpot.inventory.stackSize >= spawnSpot.inventory.maxStackSize) then
			-- We should stop this job because it's impossible to make any more items
			furniture.CancelJobs()
		end

		return
	end

	-- If we get here we have no current job. Check to see if our destination is full
	if (spawnSpot.inventory != nil and spawnSpot.inventory.stackSize >= spawnSpot.inventory.maxStackSize) then
		-- We are full, don't make a job
		return
	end

	-- If we get here we need to create a new job
	jobSpot = furniture.GetJobSpotTile()

	j = Job.__new(
		jobSpot,
		nil,
		nil,
		1,
		nil,
		true    -- This job repeats until the destination tile is full
	)
	j.RegisterJobCompletedCallback("MiningDroneStation_JobComplete")

	furniture.AddJob(j)
end

function MiningDroneStation_JobComplete(j)
	World.Current.inventoryManager.PlaceInventory(j.furniture.GetSpawnSpotTile(), Inventory.__new("Steel Plate", 50, 20))
end


return "Lua Script Parsed!"