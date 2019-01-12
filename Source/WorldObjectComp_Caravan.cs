﻿using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RoadsOfTheRim
{
    public class WorldObjectComp_Caravan : WorldObjectComp
    {
        public bool currentlyWorkingOnSite = false;
        public bool workOnWakeUp = false;

        public Caravan GetCaravan()
        {
            return (Caravan)this.parent;
        }

        public bool isThereAConstructionSiteHere()
        {
            return Find.WorldObjects.AnyWorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true), GetCaravan().Tile);
        }

        public RoadConstructionSite getSite()
        {
            try
            {
                return (RoadConstructionSite)Find.WorldObjects.WorldObjectOfDefAt(DefDatabase<WorldObjectDef>.GetNamed("RoadConstructionSite", true), ((Caravan)this.parent).Tile);
            }
            catch
            {
                return null;
            }
        }

        public bool CaravanCanWork()
        {
            Caravan caravan = GetCaravan() ;
            return (!caravan.CantMove & !caravan.pather.MovingNow) ;
        }

        public override void CompTick()
        {
            /*
            Here should be the code to handle how ally factions help construction :
            - Once help has been offered, set this.helpFromFaction = true
            - The tick at which help starts should be set as well : this.helpFromFactionStartsAt = int
            - This compTick should also check whether or not the faction is still allied, and immediately reset help to false if they're not (plus send a letter)
            - The amount of help given should be set here as well, and decreased as part of the doSomeWork() in CompRoadsOfTheRimConstructionSite
            - Therefore, doSomeWork() will be the functionality that handles resetting help
            - The cost in goodwill should decrease once work is over. If it's decreased at the beginning, there's a risk the faction wouldn't be an ally any more
            - Somewhere at high level (in the mod ? Is it possible ?) I should store a dictionary whenCanFactionHelp <Faction , int> to set some cooldown (MTB)
            */
            if (Find.TickManager.TicksGame % 100 == 0)
            {
                Caravan caravan = GetCaravan();
                // Wake up the caravan if it was nightresting
                if (this.workOnWakeUp && !caravan.NightResting)
                {
                    this.workOnWakeUp = false;
                    this.currentlyWorkingOnSite = true;
                }

                // Do some work
                if (this.currentlyWorkingOnSite & isThereAConstructionSiteHere() & CaravanCanWork())
                {
                    RoadConstructionSite TheSite = getSite();
                    bool workDone = TheSite.GetComponent<CompRoadsOfTheRimConstructionSite>().doSomeWork(caravan);
                    base.CompTick();
                    if (workDone)
                    {
                        stopWorking() ;
                        Find.World.worldObjects.Remove(TheSite);
                    }
                }

                // Stop working as soon as the caravan moves, or rests, or is downed
                if (this.currentlyWorkingOnSite & !CaravanCanWork())
                {
                    this.currentlyWorkingOnSite = false ;
                    // If the caravan is resting, stop working but remember to restart working on wake up
                    if (caravan.NightResting)
                    {
                        this.workOnWakeUp = true;
                    }
                }
            }
        }

        //Start working on a Construction Site.
        public void startWorking()
        {
            if (CaravanCanWork())
            {
                Caravan caravan = GetCaravan();
                caravan.pather.StopDead();
                this.currentlyWorkingOnSite = true ;
            }
            else
            {
                Log.Message("[Roads of the Rim] : Caravan was given the order to start working but can't work.");
            }
        }
        
        //Stop working on a Construction Site. No need to check anything, just stop
        public void stopWorking()
        {
            this.currentlyWorkingOnSite = false ;
        }

        /*
         * Amount of work :
         * - Construction speed (0.5 + 0.15 per level) times the construct success chance (0.75 to 1.13 - lvl 8 is 1)
         * - Pack animals help as well (see below)
         */
        public int amountOfWork()
        {
            Caravan caravan = (Caravan)this.parent;
            float totalConstruction = 0f;
            float animalConstruction = 0f;
            foreach (Pawn pawn in caravan.PawnsListForReading)
            {
                if (pawn.IsColonist)
                {
                    // DEBUG - Log.Message("[DEBUG] - amount of work calculation for " + pawn.Name + ": construction speed = " + (float)pawn.GetStatValue(StatDefOf.ConstructionSpeed) + ", success chance = " + (float)pawn.GetStatValue(StatDefOf.ConstructSuccessChance));
                    totalConstruction += (float)pawn.GetStatValue(StatDefOf.ConstructionSpeed) * (float)pawn.GetStatValue(StatDefOf.ConstructSuccessChance);
                }
                else if (pawn.RaceProps.packAnimal)
                {
                    animalConstruction += (float)pawn.GetStatValue(StatDefOf.ConstructionSpeed) * (float)pawn.GetStatValue(StatDefOf.ConstructSuccessChance);
                }
            }
            // Pack animals can only add as much work as humans (i.e. : at best, pack animals double the amount of work)
            if (animalConstruction > totalConstruction)
            {
                animalConstruction = totalConstruction;
            }
            totalConstruction += animalConstruction;
            // TO DO : the pawns should learn construction a little
            // TO DO : animals should help
            return (int)totalConstruction ;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref this.currentlyWorkingOnSite, "RoadsOfTheRim_Caravan_currentlyWorkingOnSite" , false , true);
            Scribe_Values.Look<bool>(ref this.workOnWakeUp, "RoadsOfTheRim_Caravan_workOnWakeUp", false, true);
        }
    }
}
