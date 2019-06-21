using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FurnitureActions
{

    public static void Door_UpdateAction(Furniture furn, float deltaTime)
    {
        //Debug.Log("Door_UpdateAction");
        if (furn.furnParameters["is_opening"] >= 1)
        {
            furn.furnParameters["openness"] += deltaTime;
            if (furn.furnParameters["openness"] >= 1)
            {
                furn.furnParameters["is_opening"] = 0;
            }
        }
        else
        {
            furn.furnParameters["openness"] -= deltaTime;
        }

        furn.furnParameters["openness"] = Mathf.Clamp01(furn.furnParameters["openness"]);
    }

    public static ENTERABILITY Door_IsEnterable(Furniture furn)
    {
        furn.furnParameters["is_opening"] = 1;

        if (furn.furnParameters["openness"] >= 1)
        {
            return ENTERABILITY.Yes;
        }

        return ENTERABILITY.Soon;
    }

}
