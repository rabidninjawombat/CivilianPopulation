using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP;

namespace CivilianManagment
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class KSPAssemblyDependency : Attribute
    {
        public string name;
        public int versionMajor;
        public int versionMinor;
        
        public extern KSPAssemblyDependency(string name, int versionMajor, int versionMinor);
    }



    public class MovieTheater : BaseConverter
    {
        [KSPField(isPersistant = false, guiActive = false)]
        public float pilotBonus = .10f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float engineerBonus = .10f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float scientistBonus = .10f;

        [KSPField(isPersistant = false, guiActive = false)]
        public float RomanceMovieBonus = .05f;

        [KSPField(isPersistant = false, guiActive = false)]
        public string InspirationResourceName = "inspiration";

        [KSPField(isPersistant = true, guiActive = true, guiName = "Movie Type Playing")]
        public string MovieType = "none";
        [KSPField(isPersistant = true, guiActive = true, guiName = "Bonus")]
        public string MovieBonus = "none";

        void resetPartInspiration()
        {
            /*  CHECK THIS OUT FOR BUG  */

            this.part.RequestResource(InspirationResourceName, 99999999, ResourceFlowMode.NO_FLOW); //reset this part's resource only

        }
        #region regolith
        protected override float GetHeatMultiplier(ConverterResults result, double deltaTime)
        {
            return 0f;
        }

        [KSPField]
        public string RecipeInputs = "";

        [KSPField]
        public string RecipeOutputs = "";

        [KSPField]
        public string RequiredResources = "";

        public ConversionRecipe Recipe
        {
            get { return _recipe ?? (_recipe = LoadRecipe()); }
        }

        private ConversionRecipe _recipe;
        protected override ConversionRecipe PrepareRecipe(double deltatime)
        {

            if (_recipe == null)
                _recipe = LoadRecipe();
            UpdateConverterStatus();
            if (!IsActivated)
                return null;
            return _recipe;
        }

        private ConversionRecipe LoadRecipe()
        {
            var r = new ConversionRecipe();
            try
            {

                if (!String.IsNullOrEmpty(RecipeInputs))
                {
                    var inputs = RecipeInputs.Split(',');
                    for (int ip = 0; ip < inputs.Length; ip += 2)
                    {
                        print(String.Format("[REGOLITH] - INPUT {0} {1}", inputs[ip], inputs[ip + 1]));
                        r.Inputs.Add(new ResourceRatio
                        {
                            ResourceName = inputs[ip].Trim(),
                            Ratio = Convert.ToDouble(inputs[ip + 1].Trim())
                        });
                    }
                }

                if (!String.IsNullOrEmpty(RecipeOutputs))
                {
                    var outputs = RecipeOutputs.Split(',');
                    for (int op = 0; op < outputs.Length; op += 3)
                    {
                        print(String.Format("[REGOLITH] - OUTPUTS {0} {1} {2}", outputs[op], outputs[op + 1],
                            outputs[op + 2]));
                        r.Outputs.Add(new ResourceRatio
                        {
                            ResourceName = outputs[op].Trim(),
                            Ratio = Convert.ToDouble(outputs[op + 1].Trim()),
                            DumpExcess = Convert.ToBoolean(outputs[op + 2].Trim())
                        });
                    }
                }

                if (!String.IsNullOrEmpty(RequiredResources))
                {
                    var requirements = RequiredResources.Split(',');
                    for (int rr = 0; rr < requirements.Length; rr += 2)
                    {
                        print(String.Format("[REGOLITH] - REQUIREMENTS {0} {1}", requirements[rr], requirements[rr + 1]));
                        r.Requirements.Add(new ResourceRatio
                        {
                            ResourceName = requirements[rr].Trim(),
                            Ratio = Convert.ToDouble(requirements[rr + 1].Trim()),
                        });
                    }
                }
            }
            catch (Exception)
            {
                print(String.Format("[REGOLITH] Error performing conversion for '{0}' - '{1}' - '{2}'", RecipeInputs, RecipeOutputs, RequiredResources));
            }
            return r;
        }

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            var recipe = LoadRecipe();
            sb.Append(".");
            sb.Append("\n");
            sb.Append(ConverterName);
            sb.Append("\n\n<color=#99FF00>Inputs:</color>");
            foreach (var input in recipe.Inputs)
            {
                sb.Append("\n - ")
                    .Append(input.ResourceName)
                    .Append(": ");
                if (input.Ratio < 0.0001)
                {
                    sb.Append(String.Format("{0:0.00}", input.Ratio * 21600)).Append("/day");
                }
                else if (input.Ratio < 0.01)
                {
                    sb.Append(String.Format("{0:0.00}", input.Ratio * 3600)).Append("/hour");
                }
                else
                {
                    sb.Append(String.Format("{0:0.00}", input.Ratio)).Append("/sec");
                }

            }
            sb.Append("\n<color=#99FF00>Outputs:</color>");
            foreach (var output in recipe.Outputs)
            {
                sb.Append("\n - ")
                    .Append(output.ResourceName)
                    .Append(": ");
                if (output.Ratio < 0.0001)
                {
                    sb.Append(String.Format("{0:0.00}", output.Ratio * 21600)).Append("/day");
                }
                else if (output.Ratio < 0.01)
                {
                    sb.Append(String.Format("{0:0.00}", output.Ratio * 3600)).Append("/hour");
                }
                else
                {
                    sb.Append(String.Format("{0:0.00}", output.Ratio)).Append("/sec");
                }
            }
            if (recipe.Requirements.Any())
            {
                sb.Append("\n<color=#99FF00>Requirements:</color>");
                foreach (var output in recipe.Requirements)
                {
                    sb.Append("\n - ")
                        .Append(output.ResourceName)
                        .Append(": ");
                    sb.Append(String.Format("{0:0.00}", output.Ratio));
                }
            }
            sb.Append("\n");
            return sb.ToString();
        }



        #endregion



        public override void OnUpdate()
        {
            Actions["StartResourceConverterAction"].active = false;
            Actions["StopResourceConverterAction"].active = false;
            base.OnUpdate();
        }
        public override void OnStart(PartModule.StartState state)
        {


            base.OnStart(state);

            Events["StartResourceConverter"].active = false;
            Events["StopResourceConverter"].active = false;


        }

        [KSPEvent(guiName = "Play Racing Movies (engineer bonus)", active = true, guiActive = true)]
        public void playRacingMovies()
        {
            resetPartInspiration();
            MovieType = "Racing Movies";
            MovieBonus = "-10% Engineer recruitment cost";
            StartResourceConverter();
            Events["StartResourceConverter"].active = false;
            Events["StopResourceConverter"].active = false;

        }
        [KSPEvent(guiName = "Play Scifi Movies (pilot bonus)", active = true, guiActive = true)]
        public void playScifi()
        {
            resetPartInspiration();
            MovieType = "Scifi Movies";
            MovieBonus = "-10% Pilot recruitment cost";
            StartResourceConverter();
            Events["StartResourceConverter"].active = false;
            Events["StopResourceConverter"].active = false;

        }
        [KSPEvent(guiName = "Play Documentaries (scientist bonus)", active = true, guiActive = true)]
        public void playDocumentaries()
        {
            resetPartInspiration();
            MovieType = "Documentaries";
            MovieBonus = "-10% Scientist recruitment cost";
            StartResourceConverter();
            Events["StartResourceConverter"].active = false;
            Events["StopResourceConverter"].active = false;

        }
       [KSPEvent(guiName = "Play Romance Movies", active = true, guiActive = true)]
        public void playRomance()
        {
            resetPartInspiration();
            MovieType = "Romance";
            MovieBonus = "10% Bonus to Civilian Reproduction";
            StartResourceConverter();
            Events["StartResourceConverter"].active = false;
            Events["StopResourceConverter"].active = false;
        }
       
        
        [KSPEvent(guiName = "Close Movie Theater", active = true, guiActive = true)]
        public void closeTheater()
        {
            resetPartInspiration();
            MovieType = "None";
            MovieBonus = "No bonus";
            StopResourceConverter();
            Events["StartResourceConverter"].active = false;
            Events["StopResourceConverter"].active = false;

        }


    }
    //this class regulates the civilian population on a part.



    public class CivilianPopulationRegulator : BaseConverter
    {
        //like the dictionary
        //the food resource name is the name of the "food" resource, which is required to grow or shrink a kerbal.
        [KSPField(isPersistant = false, guiActive = false)]
        public string foodResourceName;

        [KSPField(isPersistant = false, guiActive = false)]
        public string needWasteCorrespondence;

        [KSPField(isPersistant = false, guiActive = false)]
        public string civilianNeeds;

        [KSPField(isPersistant = false, guiActive = false)]
        public string civilianWastes;

        [KSPField(isPersistant = false, guiActive = false)]
        public float kerbalMass = 105.0f;

        [KSPField(isPersistant = false, guiActive = false)]
        public float foodPerPop;  //this is how much food is needed for the population to grow, and how much is generated when the population shrinks
        //this is calculated now, based on kerbal mass.
        //new civie pop can be recruited at this rate of food per population.  open resource system has a food definition.  That wil lbe fine

        [KSPField(isPersistant = true, guiActive = true, guiName = "Rent")]
        public float taxes = 200;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Time until Rent payment")]
        public float TimeUntilTaxes = 21600;

        [KSPField(isPersistant = false, guiActive = false)]
        public string consumptionSpeed;  //this is how long it takes for a civilian kerbal to consume 1kg of each resource


        [KSPField(isPersistant = false, guiActive = false)]
        public string populationResourceName;

        [KSPField(isPersistant = false, guiActive = true)]
        public float populationDecayRate;  //how fast does the civie population jump ship when there's no food?

        [KSPField(isPersistant = false, guiActive = true)]
        public float populationGrowthRate;  //how fast does the civie population go to ship when there's food?
        [KSPField(isPersistant = false, guiActive = false)]
        public float reproductionRate;

        [KSPField(isPersistant = true, guiActive = true)]
        public float growthRate;  //show the player how much food their population is consuming


        [KSPField(isPersistant = true, guiActive = true)]
        public float populationGrowthTimer;  //how fast does the civie population jump ship when there's no food?

        [KSPField(isPersistant = true, guiActive = true)]
        public float populationDecayTimer;  //how fast does the civie population jump ship when there's no food?

        [KSPField(isPersistant = true, guiActive = false)]
        public float CostToReproduce;  //show the player how much food their population is consuming

        [KSPField(isPersistant = true, guiActive = true)]
        public bool civilianDock;

        [KSPField(isPersistant = true, guiActive = false)]
        public float civilianDockGrowthRate;

        [KSPField(isPersistant = true, guiActive = false)]
        public float deltaEnergy;  //total energy generation on the ship

        [KSPField(isPersistant = true, guiActive = false)]
        public float lastTime;
        //only one part with this can be the master on any vessel.
        //this prevents duplicating the population calculation
        public bool master = false;
        public bool slave = false;
        //farm generators will generate the food resource
        //so long as the food resource is higher than the foodPerPop combined with the total amount of population resource
        //the population can increase.
        //if the food is less than the population resource then the population will decay over time.
        //whenever TAC LS updates, update these!
        //warning: only default balance rate supported! until i can read configs


        //if the ship was defocused over a long time calculate where things are
        // double planetariumIntegration = 0;
        bool UsePT = false;
        double calculateRateOverTime()
        {
            double pt = Planetarium.GetUniversalTime();
            if (lastTime == 0)
                lastTime = (float)pt;

            double ret = pt - lastTime;
            double dt = TimeWarp.deltaTime;

            if (!UsePT)
            {

                ret = dt;
            }



            lastTime = (float)pt;
            return ret;

        }

        float getRecruitmentRate()
        {
            ////print(FlightGlobals.currentMainBody.name);
            switch (FlightGlobals.currentMainBody.name)
            {
                case "Kerbin":
                    break;
                case "Mun":
                    return civilianDockGrowthRate / 10;
                case "Minmus":
                    return civilianDockGrowthRate / 50;
                default:
                    return 0;
            }
            return civilianDockGrowthRate;
        }


        CivilianPopulationRegulator getMaster()
        {
            var partsWithCivies = vessel.FindPartModulesImplementing<CivilianPopulationRegulator>();
            CivilianPopulationRegulator foundMaster = null;
            foreach (CivilianPopulationRegulator p in partsWithCivies)
            {

                if (p.master)
                {
                    if (foundMaster != null)
                    {
                        p.slave = true;
                        p.master = false;
                    }
                    else
                        foundMaster = p;
                }




            }
            return foundMaster;
        }


        public override void OnStart(PartModule.StartState state)
        {

            if (!HighLogic.LoadedSceneIsFlight)
            {
                base.OnStart(state);
                return;
            }
            if (this.vessel == null)
            {
                base.OnStart(state);
                return;
            }
            if (master || slave)
            {
                base.OnStart(state);
                return;
            }

            master = true;
            var partsWithCivies = vessel.FindPartModulesImplementing<CivilianPopulationRegulator>();
            foreach (CivilianPopulationRegulator p in partsWithCivies)
            {
                if (p.master)
                    continue;
                p.slave = true;
                if (p.civilianDock)
                {
                    civilianDock = p.civilianDock;
                    civilianDockGrowthRate = p.civilianDockGrowthRate;
                }

            }
            StartResourceConverter();


            base.OnStart(state);
        }
        bool getTheaterBonus()
        {
            var theaters = vessel.FindPartModulesImplementing<MovieTheater>();
            foreach (MovieTheater t in theaters)
                if (t.MovieType == "Romance")
                    return true;
            return false;
        }

        #region regolith


        protected override float GetHeatMultiplier(ConverterResults result, double deltaTime)
        {
            return 0f;
        }


        [KSPField]
        public string RecipeInputs = "";

        [KSPField]
        public string RecipeOutputs = "";

        [KSPField]
        public string RequiredResources = "";


        public ConversionRecipe Recipe
        {
            get { return _recipe ?? (_recipe = LoadRecipe()); }
        }

        private ConversionRecipe _recipe;
        protected override ConversionRecipe PrepareRecipe(double deltatime)
        {

            if (_recipe == null)
                _recipe = LoadRecipe();
            UpdateConverterStatus();
            if (!IsActivated)
                return null;
            return _recipe;
        }

        private ConversionRecipe LoadRecipe()
        {
            var r = new ConversionRecipe();
            try
            {

                if (!String.IsNullOrEmpty(RecipeInputs))
                {
                    var inputs = RecipeInputs.Split(',');
                    for (int ip = 0; ip < inputs.Count(); ip += 2)
                    {
                        print(String.Format("[REGOLITH] - INPUT {0} {1}", inputs[ip], inputs[ip + 1]));
                        r.Inputs.Add(new ResourceRatio
                        {
                            ResourceName = inputs[ip].Trim(),
                            Ratio = Convert.ToDouble(inputs[ip + 1].Trim())
                        });
                    }
                }

                if (!String.IsNullOrEmpty(RecipeOutputs))
                {
                    var outputs = RecipeOutputs.Split(',');
                    for (int op = 0; op < outputs.Count(); op += 3)
                    {
                        print(String.Format("[REGOLITH] - OUTPUTS {0} {1} {2}", outputs[op], outputs[op + 1],
                            outputs[op + 2]));
                        r.Outputs.Add(new ResourceRatio
                        {
                            ResourceName = outputs[op].Trim(),
                            Ratio = Convert.ToDouble(outputs[op + 1].Trim()),
                            DumpExcess = Convert.ToBoolean(outputs[op + 2].Trim())
                        });
                    }
                }

                if (!String.IsNullOrEmpty(RequiredResources))
                {
                    var requirements = RequiredResources.Split(',');
                    for (int rr = 0; rr < requirements.Count(); rr += 2)
                    {
                        print(String.Format("[REGOLITH] - REQUIREMENTS {0} {1}", requirements[rr], requirements[rr + 1]));
                        r.Requirements.Add(new ResourceRatio
                        {
                            ResourceName = requirements[rr].Trim(),
                            Ratio = Convert.ToDouble(requirements[rr + 1].Trim()),
                        });
                    }
                }
            }
            catch (Exception)
            {
                print(String.Format("[REGOLITH] Error performing conversion for '{0}' - '{1}' - '{2}'", RecipeInputs, RecipeOutputs, RequiredResources));
            }
            return r;
        }

        public override string GetInfo()
        {
            StringBuilder sb = new StringBuilder();
            var recipe = LoadRecipe();
            sb.Append(".");
            sb.Append("\n");
            sb.Append(ConverterName);
            sb.Append("\n\n<color=#99FF00>Inputs:</color>");
            foreach (var input in recipe.Inputs)
            {
                sb.Append("\n - ")
                    .Append(input.ResourceName)
                    .Append(": ");
                if (input.Ratio < 0.0001)
                {
                    sb.Append(String.Format("{0:0.00}", input.Ratio * 21600)).Append("/day");
                }
                else if (input.Ratio < 0.01)
                {
                    sb.Append(String.Format("{0:0.00}", input.Ratio * 3600)).Append("/hour");
                }
                else
                {
                    sb.Append(String.Format("{0:0.00}", input.Ratio)).Append("/sec");
                }

            }
            sb.Append("\n<color=#99FF00>Outputs:</color>");
            foreach (var output in recipe.Outputs)
            {
                sb.Append("\n - ")
                    .Append(output.ResourceName)
                    .Append(": ");
                if (output.Ratio < 0.0001)
                {
                    sb.Append(String.Format("{0:0.00}", output.Ratio * 21600)).Append("/day");
                }
                else if (output.Ratio < 0.01)
                {
                    sb.Append(String.Format("{0:0.00}", output.Ratio * 3600)).Append("/hour");
                }
                else
                {
                    sb.Append(String.Format("{0:0.00}", output.Ratio)).Append("/sec");
                }
            }
            if (recipe.Requirements.Any())
            {
                sb.Append("\n<color=#99FF00>Requirements:</color>");
                foreach (var output in recipe.Requirements)
                {
                    sb.Append("\n - ")
                        .Append(output.ResourceName)
                        .Append(": ");
                    sb.Append(String.Format("{0:0.00}", output.Ratio));
                }
            }
            sb.Append("\n");
            return sb.ToString();
        }

        #endregion

        double calculateRent(double x)
        {
            //up to 50 people the rent is 200 per

            double y = x;
            double rent = 200;
            double totalRent = 0;
            while (y > 0)
            {
                totalRent += rent;
                if (totalRent > 5000)
                    rent -= 1;
                if (rent < 10)
                    rent = 10;  //minimum rent
                y--;
            }


            return totalRent;
            //y = mx + b

        }

        void applyCalculations(double dt)
        {
            //  print("apstart");
            double foodbudget = base.ResBroker.AmountAvailable(this.part, foodResourceName, dt, "ALL_VESSEL");//getResourceBudget(foodResourceName);
            // PartResourceDefinition def = new PartResourceDefinition(foodResourceName);
            // print("1");
            //mass = unit * density
            //mass / density = unit
            double foodRequired = kerbalMass / 0.28102905982906; //lol density of food in kg
            double currentPop = base.ResBroker.AmountAvailable(this.part, populationResourceName, dt, "ALL_VESSEL");//getResourceBudget(populationResourceName);
            var inKerbinSOI = (part.vessel.mainBody.name == "Kerbin");
            var inMinmusSoi = (part.vessel.mainBody.name == "Minmus");
            var inMunSoi = (part.vessel.mainBody.name == "Mun");
            growthRate = (float)(currentPop / reproductionRate);
            if (getTheaterBonus())
                growthRate += growthRate * .10f;
            
            if (growthRate < 1)  //can't grow population unless it's big enough
            { 
                growthRate = 0;
                }
            // print("2");
            if (inKerbinSOI || inMinmusSoi || inMunSoi)
            {
                if (civilianDock)
                {
                    foodRequired = 0; //new kerbals arriving don't need to spend food to grow
                    growthRate = getRecruitmentRate(); //kerbal recruitment much faster than reproduction
                }
            }
            // print("3");
            bool needsMet = true;

            //calculate each requirement!
            //needs and wastes are already calibrated for conservation of mass
            //just see if we scaled one
            //Missing inputs
            // Debug.Log(base.status + " " + dt.ToString() + base.RecipeInputs);
            double decayMultiplier = 1;
            needsMet = !base.status.Contains("missing");


            if (needsMet)
            {
                //  print("3b");
                populationGrowthTimer += (float)dt * (float)growthRate;
                populationDecayTimer = 0;
                TimeUntilTaxes -= (float)dt;
                float taxesAcquired = (float)calculateRent(currentPop); //100 funds per civilian
                taxes = taxesAcquired;
                //  print("3c");
                if (TimeUntilTaxes < 0)
                {
                    if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
                    {


                        Funding.Instance.AddFunds(taxes, TransactionReasons.Vessels);
                        
                    }
                    TimeUntilTaxes = 21600;
                    
                }


            }

            else
            {
                populationDecayTimer += (float)dt * (float)decayMultiplier;
                populationGrowthTimer = 0;
            }
            //  print("4");
            while (populationGrowthTimer > populationGrowthRate && foodbudget > foodRequired) //population can grow!
            {
                double popMade = this.part.RequestResource(populationResourceName, -1);
                populationGrowthTimer -= populationGrowthRate;
                if (popMade < 0)
                    this.part.RequestResource(foodResourceName, foodRequired);

            }
            // print("5");
            while (populationDecayTimer > populationDecayRate && currentPop > 0) //population can shrink!  if there's no room to store the resources
            //of the dead kerbal part of him gets ejected into space.  Sucks to be wasteful!
            {
                double popMade = this.part.RequestResource(populationResourceName, 1);
                populationDecayTimer -= populationDecayRate;
                if (popMade > 0)
                    this.part.RequestResource(foodResourceName, -foodRequired);

            }

            SetEfficiencyBonus((float)currentPop * 1 / 21600);

            CostToReproduce = (float)foodRequired;
        }
        protected double GetDeltaTimex()
        {
            try
            {
                if (Time.timeSinceLevelLoad < 1.0f || !FlightGlobals.ready)
                {
                    return -1;
                }

                if (Math.Abs(lastUpdateTime) < float.Epsilon)
                {
                    // Just started running
                    // lastUpdateTime = Planetarium.GetUniversalTime();
                    return -1;
                }

                var deltaTime = Math.Min(Planetarium.GetUniversalTime() - lastUpdateTime, ResourceUtilities.GetMaxDeltaTime());
                //lastUpdateTime += deltaTime;
                return deltaTime;
            }
            catch (Exception e)
            {
                print("[REGO] - Error in - BaseConverter_GetDeltaTime - " + e.Message);
                return 0;
            }
        }
        public override void OnFixedUpdate()
        {

            if (slave)
            {
                //base.OnUpdate();
                var xmaster = getMaster();
                if (xmaster == null)
                {
                    master = true;
                    slave = false;
                    StartResourceConverter();
                }
                else
                {
                    //update our display numbers
                    growthRate = xmaster.growthRate;
                    populationDecayRate = xmaster.populationDecayRate;
                    populationDecayTimer = xmaster.populationDecayTimer;
                    CostToReproduce = xmaster.CostToReproduce;
                    populationGrowthRate = xmaster.populationGrowthRate;
                    populationGrowthTimer = xmaster.populationGrowthTimer;
                    TimeUntilTaxes = xmaster.TimeUntilTaxes;
                    taxes = xmaster.taxes;
                    StopResourceConverter();
                    return;
                }

            }

            double dt = GetDeltaTimex();
            applyCalculations(dt);
            base.OnFixedUpdate();

            double currentPop = getResourceBudget(populationResourceName);
            // if (currentPop > 0)

            //else
            //  

            // UsePT = false;
            // base.OnUpdate();
        }
        double getResourceBudget(string name)
        {
            //   
            if (this.vessel != null)
            {
                // ////print("found vessel event!");
                var resources = vessel.GetActiveResources();
                for (int i = 0; i < resources.Count; i++)
                {
                    // //print("vessel has resources!");
                    //print(resources[i].info.name);
                    // //print("im looking for " + resourceName);
                    if (resources[i].info.name == name)
                    {
                        // //print("Found the resouce!!");
                        return (double)resources[i].amount;

                    }
                }
            }
            return 0;
        }

        double getResourceBudgetMax(string name)
        {
            //   
            if (this.vessel != null)
            {
                // ////print("found vessel event!");
                var resources = vessel.GetActiveResources();
                for (int i = 0; i < resources.Count; i++)
                {
                    // //print("vessel has resources!");
                    //print(resources[i].info.name);
                    // //print("im looking for " + resourceName);
                    if (resources[i].info.name == name)
                    {
                        // //print("Found the resouce!!");
                        return (double)resources[i].maxAmount;

                    }
                }
            }
            return 0;
        }

    }

    public class KerbalRecruiter : PartModule
    {
        [KSPField(isPersistant = false, guiActive = false)]
        public float civilianPopulationCost;//{ get; set; }
        [KSPField(isPersistant = false, guiActive = false)]
        public string populationName;//{ get; set; }

        [KSPField(isPersistant = false, guiActive = false)]
        public float foodCost = 21;//{ get; set; }
        [KSPField(isPersistant = false, guiActive = false)]
        public string foodName = "Food";//{ get; set; }


        [KSPField(isPersistant = false, guiActive = false)]
        public bool allowEngineerScientist = false;
        [KSPField(isPersistant = false, guiActive = false)]
        public bool allowPilot = false;

        [KSPField(isPersistant = false, guiActive = false)]
        public bool university = false;
        [KSPField(isPersistant = false, guiActive = false)]
        public bool flightschool = false;

        [KSPField(isPersistant = false, guiActive = false)]
        public string flightExperienceResourceName;
        [KSPField(isPersistant = false, guiActive = false)]
        public float flightExperienceCost;

        [KSPField(isPersistant = false, guiActive = false)]
        public string educationResourceName;
        [KSPField(isPersistant = false, guiActive = false)]
        public float educationCost;
        [KSPField(isPersistant = false, guiActive = false)]
        public int xplevel;


        [KSPField(isPersistant = false, guiActive = false)]
        public string inspirationResourceName;
        [KSPField(isPersistant = false, guiActive = false)]
        public float inspirationCost;
        [KSPField(isPersistant = false, guiActive = false)]
        public string career;

        public static void screenMessage(string msg)
        {
            ScreenMessage smg = new ScreenMessage(msg, 4.0f, ScreenMessageStyle.UPPER_CENTER);
            ScreenMessages.PostScreenMessage(smg);
        }

        double applyTheaterBonus(double cost, KerbalJob job)
        {
            var theaters = vessel.FindPartModulesImplementing<MovieTheater>();
            Debug.Log(theaters.Count);


            foreach (MovieTheater t in theaters)
            {
                Debug.Log(t.MovieType);
                if (t.MovieType == "Racing Movies" && job == KerbalJob.Engineer)
                    return cost - cost * (t.engineerBonus);
                if (t.MovieType == "Scifi Movies" && job == KerbalJob.Pilot)
                    return cost - cost * (t.pilotBonus);
                if (t.MovieType == "Documentaries" && job == KerbalJob.Scientist)
                    return cost - cost * (t.scientistBonus);

            }
            return cost;

        }


        enum KerbalJob
        {
            Pilot,
            Engineer,
            Scientist
        }

        ProtoCrewMember CreateRandomKerbal()
        {
            var roster = HighLogic.CurrentGame.CrewRoster;

            var newMember = roster.GetNewKerbal(ProtoCrewMember.KerbalType.Crew);
            return newMember;
        }

        ProtoCrewMember CreateKerbal(KerbalJob job, int startLevel)
        {
            var roster = HighLogic.CurrentGame.CrewRoster;

            var newMember = roster.GetNewKerbal(ProtoCrewMember.KerbalType.Crew);

            //need to make an experience trait specifically!
            
            
            while (newMember.experienceTrait.Title != career)
            {
                HighLogic.CurrentGame.CrewRoster.Remove(newMember);
                newMember = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Crew);
                
            }
                     


            float startXP = KerbalRoster.GetExperienceLevelRequirement(startLevel);

            if (xplevel == 3)
            {
                newMember.flightLog.AddEntry("Orbit,Kerbin");
                newMember.flightLog.AddEntry("Suborbit,Kerbin");
                newMember.flightLog.AddEntry("Flight,Kerbin");
                newMember.flightLog.AddEntry("Land,Kerbin");
                newMember.flightLog.AddEntry("Recover");
                newMember.flightLog.AddEntry("Flyby,Mun");
                newMember.flightLog.AddEntry("Orbit,Mun");
                newMember.flightLog.AddEntry("Land,Mun");
                newMember.flightLog.AddEntry("Flyby,Minmus");
                newMember.flightLog.AddEntry("Orbit,Minmus");
                newMember.flightLog.AddEntry("Land,Minmus");
                newMember.flightLog.AddEntry("Flyby,Sun");
                newMember.ArchiveFlightLog();
                newMember.experience = 8;
                newMember.experienceLevel = 3;
            }
            else
            {
                newMember.flightLog.AddEntry("Orbit,Kerbin");
                newMember.flightLog.AddEntry("Suborbit,Kerbin");
                newMember.flightLog.AddEntry("Flight,Kerbin");
                newMember.flightLog.AddEntry("Land,Kerbin");
                newMember.flightLog.AddEntry("Recover");
                newMember.ArchiveFlightLog();
                newMember.experience = 3;
                newMember.experienceLevel = 1;
            }
            
            return newMember;
            
        }



        [KSPEvent(guiName = "Recruit Kerbal", active = true, guiActive = true)]
        public void RecruitKerbal()
        {
            //generate a random kerbal and populate him into this ship

            //do we have enough pop to make a kerbal?
            float budget = getResourceBudget(populationName);
            if (budget < civilianPopulationCost)
            {
                screenMessage("No civilians to recruit");
                return;
            }
            budget = getResourceBudget(foodName);

            if (vessel.GetCrewCount() < vessel.GetCrewCapacity())
            {
                //get one available crew from the roster who's unassigned

                var newMember = CreateRandomKerbal();
                print(newMember.experienceTrait.TypeName);
                print(newMember.experience);
                print(newMember.experienceLevel);



                bool addSuccess = part.AddCrewmember(newMember);
                if (addSuccess)
                {
                    part.RequestResource(populationName, civilianPopulationCost); //spend the population if we have it.

                }

            }



        }

        [KSPEvent(guiName = "Recruit Pilot", active = true, guiActive = true)]
        public void RecruitPilotKerbal()
        {
            career = "Pilot";
            float budget = getResourceBudget(populationName);
            if (budget < civilianPopulationCost)
            {
                screenMessage("No civilians to recruit");
                return;
            }

            xplevel = 1;
            //if this is a flight school recruitment requires 5000 flight experience
            double flightXP = getResourceBudget(flightExperienceResourceName);
            double tempFlightXPCost = applyTheaterBonus(flightExperienceCost, KerbalJob.Pilot);
            if (flightschool)
            {

                Debug.Log("flight cost: " + tempFlightXPCost.ToString());
                if (flightXP < tempFlightXPCost)
                {
                    screenMessage("Not enough flight experience to recruit a level 3 pilot");
                    return;
                }
                xplevel = 3;
            }
            double inspriaton = getResourceBudget(inspirationResourceName);
            double tempInspirationCost = applyTheaterBonus(inspirationCost, KerbalJob.Pilot);

            if (inspriaton < tempInspirationCost)
            {
                screenMessage("Not enough inspiration to recruit a pilot");
                return;
            }


            if (vessel.GetCrewCount() < vessel.GetCrewCapacity())
            {
                //get one available crew from the roster who's unassigned

                var newMember = CreateKerbal(KerbalJob.Pilot, xplevel);
                print(newMember.experienceTrait.TypeName);
                print(newMember.experience);
                print(newMember.experienceLevel);

                bool addSuccess = part.AddCrewmember(newMember);
                if (addSuccess)
                {
                    part.RequestResource(populationName, civilianPopulationCost); //spend the population if we have it.
                    part.RequestResource(inspirationResourceName, tempInspirationCost);
                    if (flightschool)
                        part.RequestResource(flightExperienceResourceName, tempFlightXPCost);
                }

            }

            

        }
        [KSPEvent(guiName = "Recruit Engineer", active = true, guiActive = true)]
        public void RecruitEngineerKerbal()
        {
            career = "Engineer";
            float budget = getResourceBudget(populationName);
            if (budget < civilianPopulationCost)
            {
                screenMessage("No civilians to recruit");
                return;
            }

            xplevel = 1;
            double education = getResourceBudget(educationResourceName);
            double tempEducationCost = applyTheaterBonus(educationCost, KerbalJob.Engineer);
            Debug.Log("engie cost: " + tempEducationCost.ToString());
            if (university)
            {

                if (education < tempEducationCost)
                {
                    screenMessage("Not enough education to recruit a level 3 Engineer");
                    return;
                }
                xplevel = 3;
            }
            double inspriaton = getResourceBudget(inspirationResourceName);
            double tempInspirationCost = applyTheaterBonus(inspirationCost, KerbalJob.Engineer);
            if (inspriaton < tempInspirationCost)
            {
                screenMessage("Not enough inspiration to recruit an Engineer");
                return;
            }

            if (vessel.GetCrewCount() < vessel.GetCrewCapacity())
            {
                //get one available crew from the roster who's unassigned

                var newMember = CreateKerbal(KerbalJob.Engineer, xplevel);
                print(newMember.experienceTrait.TypeName);
                print(newMember.experience);
                print(newMember.experienceLevel);

                bool addSuccess = part.AddCrewmember(newMember);
                if (addSuccess)
                {
                    part.RequestResource(populationName, civilianPopulationCost); //spend the population if we have it.
                    if (university)
                        part.RequestResource(educationResourceName, tempEducationCost);
                    part.RequestResource(inspirationResourceName, tempInspirationCost);
                }

            }



        }
        [KSPEvent(guiName = "Recruit Scientist", active = true, guiActive = true)]
        public void RecruitScienceKerbal()
        {
            career = "Scientist";
            float budget = getResourceBudget(populationName);
            if (budget < civilianPopulationCost)
            {
                screenMessage("No civilians to recruit");
                return;
            }

            xplevel = 1;

            double education = getResourceBudget(educationResourceName);
            double tempEducationCost = applyTheaterBonus(educationCost, KerbalJob.Scientist);
            if (university)
            {
                Debug.Log("science cost: " + tempEducationCost.ToString());
                if (education < tempEducationCost)
                {
                    screenMessage("Not enough education to recruit a level 3 Scientist");
                    return;
                }
                xplevel = 3;
            }
            double inspriaton = getResourceBudget(inspirationResourceName);
            double tempInspirationCost = applyTheaterBonus(inspirationCost, KerbalJob.Scientist);
            if (inspriaton < tempInspirationCost)
            {
                screenMessage("Not enough inspiration to recruit a Scientist");
                return;
            }
            if (vessel.GetCrewCount() < vessel.GetCrewCapacity())
            {
                //get one available crew from the roster who's unassigned

                var newMember = CreateKerbal(KerbalJob.Scientist, xplevel);
                print(newMember.experienceTrait.TypeName);
                print(newMember.experience);
                print(newMember.experienceLevel);

                bool addSuccess = part.AddCrewmember(newMember);
                if (addSuccess)
                {
                    part.RequestResource(populationName, civilianPopulationCost); //spend the population if we have it.
                    if (university)
                        part.RequestResource(educationResourceName, tempEducationCost);
                    part.RequestResource(inspirationResourceName, tempInspirationCost);
                }

            }



        }

        public override void OnStart(PartModule.StartState state)
        {
            if (allowEngineerScientist || allowPilot)
                Events["RecruitKerbal"].guiActive = false;
            if (!allowEngineerScientist)
                Events["RecruitEngineerKerbal"].guiActive = false;
            if (!allowEngineerScientist)
                Events["RecruitScienceKerbal"].guiActive = false;
            if (!allowPilot)
                Events["RecruitPilotKerbal"].guiActive = false;


            base.OnStart(state);
        }
        float getResourceBudget(string name)
        {
            //   
            if (this.vessel == FlightGlobals.ActiveVessel)
            {
                // //print("found vessel event!");
                var resources = vessel.GetActiveResources();
                for (int i = 0; i < resources.Count; i++)
                {
                    // //print("vessel has resources!");
                    //print(resources[i].info.name);
                    // //print("im looking for " + resourceName);
                    if (resources[i].info.name == name)
                    {
                        // //print("Found the resouce!!");
                        return (float)resources[i].amount;

                    }
                }
            }
            return 0;
        }
    }
   
}
