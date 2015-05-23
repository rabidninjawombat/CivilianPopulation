using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;





namespace PipeLines
{


    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class KSPAssemblyDependency : Attribute
    {
        public string name;
        public int versionMajor;
        public int versionMinor;

        public extern KSPAssemblyDependency(string name, int versionMajor, int versionMinor);
    }

    public class AutomatedConstruction : PartModule
    {
        [KSPField(isPersistant = false, guiActive = false)]
        public float productivityOut;//{ get; set; }

        [KSPField(isPersistant = false, guiActive = false)]
        public string resourceName;//{get; set;}

        [KSPField(isPersistant = true, guiActive = false)]
        public bool generatorActive;

        //consume this resource per game-second
        [KSPField(isPersistant = false, guiActive = false)]
        public float generatorResourceIn;
        [KSPField(isPersistant = false, guiActive = false)]
        public float generatorResourceIn2;
        //produce this resource per game second
        [KSPField(isPersistant = false, guiActive = false)]
        public float generatorResourceOut;
        [KSPField(isPersistant = false, guiActive = false)]
        public float generatorResourceOut2;
        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorResourceInName;

        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorResourceInName2;


        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorResourceOutName;

        //[KSPField(isPersistant = true, guiActive = false)]
        //public string experimentID ;//{ get; set; }
        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorActivateName;
        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorDeactivateName;

        [KSPField(isPersistant = true, guiActive = false)]
        public string currentBiome = "";
        [KSPField(isPersistant = false, guiActive = false)]
        public bool needSubjects = false;
        [KSPField(isPersistant = false, guiActive = false)]
        public string loopingAnimation = "";
        [KSPField(isPersistant = true, guiActive = false)]
        public int crewCount;
        [KSPField(isPersistant = false, guiActive = false)]
        public float loopPoint;
        private void PlayStartAnimation(Animation StartAnimation, string startAnimationName, int speed, bool instant)
        {
            if (startAnimationName != "")
            {
                if (speed < 0)
                {
                    StartAnimation[startAnimationName].time = StartAnimation[startAnimationName].length;
                    if (loopPoint != 0)
                        StartAnimation[startAnimationName].time = loopPoint;
                }
                if (instant)
                    StartAnimation[startAnimationName].speed = 999999 * speed;
                StartAnimation[startAnimationName].wrapMode = WrapMode.Default;
                StartAnimation[startAnimationName].speed = speed;
                StartAnimation.Play(startAnimationName);
            }
        }
        private void PlayLoopAnimation(Animation StartAnimation, string startAnimationName, int speed, bool instant)
        {
            if (startAnimationName != "")
            {
                // //print(StartAnimation[startAnimationName].time.ToString() + " " + loopPoint.ToString());
                if (StartAnimation[startAnimationName].time >= StartAnimation[startAnimationName].length || StartAnimation.isPlaying == false)
                {
                    StartAnimation[startAnimationName].time = loopPoint;
                    ////print(StartAnimation[startAnimationName].time.ToString() + " " + loopPoint.ToString());
                    if (instant)
                        StartAnimation[startAnimationName].speed = 999999 * speed;
                    StartAnimation[startAnimationName].speed = speed;
                    StartAnimation[startAnimationName].wrapMode = WrapMode.Default;
                    StartAnimation.Play(startAnimationName);

                }
            }

        }
        public void PlayAnimation(string name, bool rewind, bool instant, bool loop)
        {
            // note: assumes one ModuleAnimateGeneric (or derived version) for this part
            // if this isn't the case, needs fixing. That's cool, I called in the part.cfg


            {

                var anim = part.FindModelAnimators();

                foreach (Animation a in anim)
                {
                    // //print("animation found " + a.name + " " + a.clip.name);
                    if (a.clip.name == name)
                    {
                        // //print("animation playingxx " + a.name + " " + a.clip.name);
                        var xanim = a;
                        if (loop)
                            PlayLoopAnimation(xanim, name, (rewind) ? (-1) : (1), instant);
                        else
                            PlayStartAnimation(xanim, name, (rewind) ? (-1) : (1), instant);
                    }
                }

            }


        }
        [KSPEvent(guiName = "Activate Generator", active = true, guiActive = true)]
        public void activateGenerator()
        {
            generatorActive = true;
            PlayAnimation(loopingAnimation, false, false, false);


        }
        [KSPEvent(guiName = "Activate Generator", active = true, guiActive = true)]
        public void deActivateGenerator()
        {
            generatorActive = false;
            PlayAnimation(loopingAnimation, true, true, false);
        }

        public override void OnStart(PartModule.StartState state)
        {

            //this.Events["Deploy"].guiActive = false;
            Events["activateGenerator"].guiName = generatorActivateName;
            Events["deActivateGenerator"].guiName = generatorDeactivateName;

            if (generatorActive)
                PlayAnimation(loopingAnimation, false, true, false);
            else
                PlayAnimation(loopingAnimation, true, true, false);

            base.OnStart(state);
        }
        void setProductivity(float producitivity)
        {
            //todo: EPL support once it updates

            ExtraplanetaryLaunchpads.ExWorkshop moduleList = this.part.FindModuleImplementing<ExtraplanetaryLaunchpads.ExWorkshop>();
            if (moduleList != null)
            {
                //found a workshop
                ////print("Found workshop!");

                moduleList.Productivity = producitivity;
                moduleList.IgnoreCrewCapacity = true;
                ////print("Producitivity is " + moduleList.Productivity.ToString());
            }


        }
        public override void OnUpdate()
        {
            int lcrewCount = part.protoModuleCrew.Count;
            if (generatorActive)
            {
                Events["deActivateGenerator"].guiActive = true;
                Events["activateGenerator"].guiActive = false;
                //while the generator is active... update the resource based on how much game time passed
                double dt = TimeWarp.deltaTime;

                //generating a resource!

                double generatescale = 1;
                double spent = 0;
                double spent2 = 0;
                bool generateOK = false;
                if (generatorResourceIn != 0)
                {
                    spent = part.RequestResource(generatorResourceInName, generatorResourceIn * dt);
                    if(spent > 0)
                    generateOK = true;
                }
               
                //  //print(spent.ToString());
                if (generateOK)
                {
                    setProductivity(productivityOut);

                }
                else
                {
                    setProductivity(0);
                   // deActivateGenerator();
                }

               
                
                  
                 //if the resource consumed is ok then we
                







            }
            else
            {
                setProductivity(0);
                Events["deActivateGenerator"].guiActive = false;
                Events["activateGenerator"].guiActive = true;
            }

            base.OnUpdate();

        }
        public string BiomeCheck()
        {

            // bool flying = vessel.altitude < vessel.mainBody.maxAtmosphereAltitude;
            //bool orbiting = 

            //return "InspaceOver" + vessel.mainBody.name;

            string situation = vessel.RevealSituationString();
            if (situation.Contains("Landed") || situation.Contains("flight"))
                return FlightGlobals.currentMainBody.BiomeMap.GetAtt(vessel.latitude * Mathf.Deg2Rad, vessel.longitude * Mathf.Deg2Rad).name + situation;


            return situation;
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
                    ////print(resources[i].info.name);
                    // //print("im looking for " + resourceName);
                    if (resources[i].info.name == resourceName)
                    {
                        // //print("Found the resouce!!");
                        return (float)resources[i].amount;

                    }
                }
            }
            return 0;
        }
       



    }


    public class FarmGenerator : PartModule  //make food, consuming water and electricity! It's just a generator class.
    {
        //I/O resources!
        //electricCharge,1.0;food,1.0;water,1.0

        

        [KSPField(isPersistant = false, guiActive = false)]
        public string inputResources;
        [KSPField(isPersistant = false, guiActive = false)]
        public string outputResources;
        [KSPField(isPersistant = false, guiActive = false)]
        public string productionScale = "1";

        [KSPField(isPersistant = false, guiActive = false)]
        public string productionSpeed = "1";

        [KSPField(isPersistant = false, guiActive = false)]
        public bool alwaysOn = false;

        [KSPField(isPersistant = false, guiActive = false)]
        public float resourceAmount;//{ get; set; }

        [KSPField(isPersistant = false, guiActive = false)]
        public string resourceName;//{get; set;}

        [KSPField(isPersistant = true, guiActive = false)]
        public bool generatorActive;

        //consume this resource per game-second
        [KSPField(isPersistant = false, guiActive = false)]
        public float generatorResourceIn;
        [KSPField(isPersistant = false, guiActive = false)]
        public float generatorResourceIn2;
        //produce this resource per game second
        [KSPField(isPersistant = false, guiActive = false)]
        public float generatorResourceOut;
        [KSPField(isPersistant = false, guiActive = false)]
        public float generatorResourceOut2;
        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorResourceInName;

        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorResourceInName2;


        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorResourceOutName;

        //[KSPField(isPersistant = true, guiActive = false)]
        //public string experimentID ;//{ get; set; }
        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorActivateName;
        [KSPField(isPersistant = false, guiActive = false)]
        public string generatorDeactivateName;

        [KSPField(isPersistant = true, guiActive = false)]
        public string currentBiome = "";
        [KSPField(isPersistant = false, guiActive = false)]
        public bool needSubjects = false;
        [KSPField(isPersistant = false, guiActive = false)]
        public string loopingAnimation = "";
        [KSPField(isPersistant = true, guiActive = false)]
        public int crewCount;
        [KSPField(isPersistant = false, guiActive = false)]
        public float loopPoint;

        [KSPField(isPersistant = true, guiActive = false)]
        public bool generatorOK = false;  //set to true when the generator is able to make resource, false if not

        [KSPField(isPersistant = false, guiActive = false)]
        public bool  isDrill;
        [KSPField(isPersistant = false, guiActive = false)]
        public string lastAbundance;



        [KSPField(isPersistant = false, guiActive = false)]
        public float efficiencyLanded = 1.0f;

        
        [KSPField(isPersistant=false, guiActive=false)]
        public float efficiencyFlying = 1.0f;

        float resource1Cap;
        float resource2Cap;

        float resourceOutCap;
       

        Dictionary<int, double> idInputs;
        Dictionary<int, double> idOutputs;

       public Dictionary<string, double> inputs;
       public Dictionary<string, double> outputs;
       public Dictionary<string, double> resourceCaps;
        double scale;
        
        private void PlayStartAnimation(Animation StartAnimation, string startAnimationName, int speed, bool instant)
        {
            if (startAnimationName != "")
            {
                if (speed < 0)
                {
                    StartAnimation[startAnimationName].time = StartAnimation[startAnimationName].length;
                    if (loopPoint != 0)
                        StartAnimation[startAnimationName].time = loopPoint;
                }
                if (instant)
                    StartAnimation[startAnimationName].speed = 999999 * speed;
                StartAnimation[startAnimationName].wrapMode = WrapMode.Default;
                StartAnimation[startAnimationName].speed = speed;
                StartAnimation.Play(startAnimationName);
            }
        }
        private void PlayLoopAnimation(Animation StartAnimation, string startAnimationName, int speed, bool instant)
        {
            if (startAnimationName != "")
            {
                // //print(StartAnimation[startAnimationName].time.ToString() + " " + loopPoint.ToString());
                if (StartAnimation[startAnimationName].time >= StartAnimation[startAnimationName].length || StartAnimation.isPlaying == false)
                {
                    StartAnimation[startAnimationName].time = loopPoint;
                    ////print(StartAnimation[startAnimationName].time.ToString() + " " + loopPoint.ToString());
                    if (instant)
                        StartAnimation[startAnimationName].speed = 999999 * speed;
                    StartAnimation[startAnimationName].speed = speed;
                    StartAnimation[startAnimationName].wrapMode = WrapMode.Default;
                    StartAnimation.Play(startAnimationName);

                }
            }

        }
        public void PlayAnimation(string name, bool rewind, bool instant, bool loop)
        {
            // note: assumes one ModuleAnimateGeneric (or derived version) for this part
            // if this isn't the case, needs fixing. That's cool, I called in the part.cfg


            {

                var anim = part.FindModelAnimators();

                foreach (Animation a in anim)
                {
                    // //print("animation found " + a.name + " " + a.clip.name);
                    if (a.clip.name == name)
                    {
                        // //print("animation playingxx " + a.name + " " + a.clip.name);
                        var xanim = a;
                        if (loop)
                            PlayLoopAnimation(xanim, name, (rewind) ? (-1) : (1), instant);
                        else
                            PlayStartAnimation(xanim, name, (rewind) ? (-1) : (1), instant);
                    }
                }

            }


        }
        [KSPEvent(guiName = "Activate Generator", active = true, guiActive = true)]
        public void activateGenerator()
        {
            generatorActive = true;
            PlayAnimation(loopingAnimation, false, false, false);


        }
        [KSPEvent(guiName = "Activate Generator", active = true, guiActive = true)]
        public void deActivateGenerator()
        {
            generatorActive = false;
            PlayAnimation(loopingAnimation, true, true, false);
        }
        public static Dictionary<string, double> loadDictionary(string list)
        {
            Dictionary<string, double> ret = new Dictionary<string, double>();
            string[] inputList = list.Split(';');
            foreach (string s in inputList)
            {
                var parts = s.Split(',');
                ret.Add(parts[0], double.Parse(parts[1]));

            }
            return ret;
        }

        public static Dictionary<string, string> loadCorrespondenceDictionary(string list)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            string[] inputList = list.Split(';');
            foreach (string s in inputList)
            {
                var parts = s.Split(',');
                ret.Add(parts[0], parts[1]);

            }
            return ret;
        }
        public Dictionary<string, double> abundance;
        void getAbundance()
        {
           // //can we drill:  the distance between the drill and the ground must be less than 3 meters
            
           // double candrill = 1;
           // vessel.checkLanded();
           // if (!vessel.Landed)
           //     candrill = 0;

           //abundance = new Dictionary<string,double>();
           // foreach(string resource in inputs.Keys)
           // {
           //     var abRequest = new Regolith.Common.AbundanceRequest
           //     {
           //         Altitude = vessel.altitude,
           //         BodyId = FlightGlobals.currentMainBody.flightGlobalsIndex,
           //         CheckForLock = false,
           //         Latitude = vessel.latitude,
           //         Longitude = vessel.longitude,
           //         ResourceType = (Regolith.Common.HarvestTypes)0,
           //         ResourceName = resource
           //     };
           //     var fabundance = Regolith.Common.RegolithResourceMap.GetAbundance(abRequest);
           //     abundance[resource] = candrill * fabundance;


              
           // }
            

        }
        int electricChargeID = -1;

        public List<int> inputIds;
        public List<int> outputIds;
        public List<double> inputDemands;
        public List<double> outputDemands;



        public override void OnStart(PartModule.StartState state)
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                base.OnStart(state);
                return;
            }

       
            

            //this.Events["Deploy"].guiActive = false;
            Events["activateGenerator"].guiName = generatorActivateName;
            Events["deActivateGenerator"].guiName = generatorDeactivateName;
            if (alwaysOn)
                activateGenerator();
            if (generatorActive)
                PlayAnimation(loopingAnimation, false, true, false);
            else
                PlayAnimation(loopingAnimation, true, true, false);

           // resource1Cap = getResourceCap(generatorResourceInName);
           // resource2Cap = getResourceCap(generatorResourceInName2);
           // resourceOutCap = getResourceCap(generatorResourceOutName);

            //parse out the inputs and outputs for better performance
            inputs = loadDictionary(inputResources);

            outputs = loadDictionary(outputResources);

            idInputs = new Dictionary<int, double>();
            idOutputs = new Dictionary<int, double>();

            var resources = this.part.vessel.GetActiveResources();
            inputDemands = new List<double>();
            outputDemands = new List<double>();
            inputIds = new List<int>();
            outputIds = new List<int>();
            for (int i = 0; i < resources.Count; i++)
            {
                foreach (string input in inputs.Keys)
                    if (input == resources[i].info.name)
                    {
                        inputIds.Add(resources[i].info.id);
                        inputDemands.Add(inputs[input]);
                       // idInputs[resource.info.id] = inputs[input];
                    }
              foreach (string input in outputs.Keys)
                  if (input == resources[i].info.name)
                    {
                        outputIds.Add(resources[i].info.id);
                        outputDemands.Add(outputs[input]);
                            //idOutputs[resource.info.id] = outputs[input];
                    }
                if (resources[i].info.name == "ElectricCharge")
                    electricChargeID = resources[i].info.id;
                print(resources[i]);
            }

                foreach (var resource in resources)
                {
                    foreach (string input in inputs.Keys)
                        if (input == resource.info.name)
                            idInputs[resource.info.id] = inputs[input];
                    foreach (string input in outputs.Keys)
                        if (input == resource.info.name)
                            idOutputs[resource.info.id] = outputs[input];
                    if (resource.info.name == "ElectricCharge")
                        electricChargeID = resource.info.id;
                    print(resource);
                }

            double pscale = double.Parse(productionScale);
            double oscale = double.Parse(productionSpeed);  //scales the whole formula

            scale = pscale /oscale;

            base.OnStart(state);
            
           
            
        }

        public double waterSpent;  //resource 2
        public double foodGenerated; //resource out

        public void runFarm2(double dtx, bool sim)
        {
            if (!generatorActive)
                return;

            double dt = dtx * scale;
           //this is just like the other runFarm, now we scale indefinitely.
           // if (isDrill)
            //    getAbundance();

            Dictionary<int, double> budgetList = new Dictionary<int, double>();
            Dictionary<int, double> capList = new Dictionary<int, double>();
            var resources = vessel.GetActiveResources();
            for (int i = 0; i < resources.Count; i++)
            {
                budgetList[resources[i].info.id] = resources[i].amount;
                capList[resources[i].info.id] = resources[i].maxAmount;
            }

            double[] inputscalars = new double[inputIds.Count];
            double[] intendedExpense = new double[inputIds.Count];
            for (int i = 0; i < inputDemands.Count; i++ )
            {
                //determine the input vs the max spendable
                double currentResource = budgetList[inputIds[i]];
                double demand = dt * inputDemands[i];
                double maxspend = Math.Min(demand, currentResource);
                inputscalars[i] = maxspend / demand;
                intendedExpense[i] = maxspend;
            }

            int lowestScalarResource = 0;
            for (int i = 0; i < inputDemands.Count; i++)
            {
                //determine the intended expense.
                if (inputscalars[i] < inputscalars[i])
                    lowestScalarResource = i;

            }
            double lowestScalar = inputscalars[lowestScalarResource];
            for (int i = 0; i < inputDemands.Count; i++)
            {
                //with the lowest scalar in hand, multiply every other resource expense by that amount.
                intendedExpense[i] = intendedExpense[i] * lowestScalar;

            }
            bool canGenerate = true;
            for (int i = 0; i < inputDemands.Count; i++)
            {
                //with the lowest scalar in hand, multiply every other resource expense by that amount.
                if (intendedExpense[i] == 0)
                    canGenerate = false;

            }
            if(canGenerate)
            {
                //when generating output resources, the generator may still be able to generate other resources
                //if that is the case, then the expense will be scaled evenly distributing the remaining resources.
                double[] intendedGeneration = new double[outputIds.Count];
                double[] generateScalar = new double[outputIds.Count];
                //each generated resource adds its wieght evenly to the expense scalar, which scales the final expenses.
                int numOutputs = outputIds.Count;
                double expenseScalar = 0;
                for (int i = 0; i < outputIds.Count; i++)
                {
                    double currentResource = budgetList[outputIds[i]];
                    double maxResource = capList[(outputIds[i])];
                    double maxGenerate = maxResource - currentResource;
                    double generateDemand = dt * outputDemands[i];
                    double intendedGenerate = Math.Min(generateDemand, maxGenerate);
                    double generateScale = intendedGenerate / generateDemand;
                     //scale the generated resource by its abundance
                    generateScalar[i] = generateScale;
                    intendedGeneration[i] = intendedGenerate;

                    expenseScalar += generateScale / numOutputs;
                }
                //scale the final expenses and apply them
                for (int i = 0; i < inputIds.Count; i++)
                {
                    double spendme = intendedExpense[i] * expenseScalar;
                    if (inputIds[i] == electricChargeID && sim) continue; //ignore electricity
                    part.RequestResource(inputIds[i], spendme);
                }
                //generate outputs
                for (int i = 0; i < outputIds.Count; i++)
                {
                    double generateme = -intendedGeneration[i];
                    part.RequestResource(outputIds[i], generateme);
                }
    
            }
           
            


        }


        public void runFarm(double dt, bool sim)
        {
            if (!generatorActive)
                return;
            bool canGenerate = true;
            double refundScalar = 1;
            Dictionary<int, double> refundList = new Dictionary<int, double>();
            foreach(int resource in idInputs.Keys)
            {
                if (resource == electricChargeID)
                    continue;
                refundList[resource] = part.RequestResource(resource, idInputs[resource]);
                double tempRefundScalar = refundList[resource] / idInputs[resource];
                if (tempRefundScalar == 0)
                    canGenerate = false;
            }
            if(canGenerate)
            {


            }



        }

        //public override void OnUpdate()
        //{
        //    base.OnUpdate();
           
        //    int lcrewCount = part.protoModuleCrew.Count;
        //    double dt = TimeWarp.fixedDeltaTime;
        //    System.Diagnostics.Trace.WriteLine("Fixed update dt " + dt.ToString() + " " + generatorActive.ToString());
        //    if (generatorActive)
        //    {
        //        Events["deActivateGenerator"].guiActive = true;
        //        Events["activateGenerator"].guiActive = false;

        //        //while the generator is active... update the resource based on how much game time passed
               
        //       // System.Diagnostics.Stopwatch sw1 = new System.Diagnostics.Stopwatch();
        //        // sw1.Start();
              
        //        // sw1.Stop();
        //        //System.Diagnostics.Trace.WriteLine("runfarm took " + sw1.Elapsed.TotalSeconds.ToString());

        //        // if (loopingAnimation != "")
        //        //  PlayAnimation(loopingAnimation, false, false, true); //plays independently of other anims


        //    }
        //    else
        //    {
        //        Events["deActivateGenerator"].guiActive = false;
        //       Events["activateGenerator"].guiActive = true;
        //        generatorOK = false;
        //    }
        //    if (alwaysOn)
        //    {
        //        Events["deActivateGenerator"].guiActive = false;
        //        Events["activateGenerator"].guiActive = false;
        //    }
          
        //}

        public override void OnUpdate()
        {
            int lcrewCount = part.protoModuleCrew.Count;
            System.Diagnostics.Trace.WriteLine("Fixed update a " + generatorActive.ToString());
            if (generatorActive)
            {
                Events["deActivateGenerator"].guiActive = true;
                Events["activateGenerator"].guiActive = false;

                //while the generator is active... update the resource based on how much game time passed
               // double dt = TimeWarp.fixedDeltaTime;
                // System.Diagnostics.Stopwatch sw1 = new System.Diagnostics.Stopwatch();
                // sw1.Start();
                double dt = TimeWarp.deltaTime;
                runFarm2(dt, false);
               // runFarm2(dt, false);
                // sw1.Stop();
                //System.Diagnostics.Trace.WriteLine("runfarm took " + sw1.Elapsed.TotalSeconds.ToString());

                // if (loopingAnimation != "")
                //  PlayAnimation(loopingAnimation, false, false, true); //plays independently of other anims


            }
            else
            {
               Events["deActivateGenerator"].guiActive = false;
                Events["activateGenerator"].guiActive = true;
                generatorOK = false;
            }
            if (alwaysOn)
            {
                Events["deActivateGenerator"].guiActive = false;
                Events["activateGenerator"].guiActive = false;
            }
            base.OnUpdate();
           


        }
        public string BiomeCheck()
        {

            // bool flying = vessel.altitude < vessel.mainBody.maxAtmosphereAltitude;
            //bool orbiting = 

            //return "InspaceOver" + vessel.mainBody.name;

            string situation = vessel.RevealSituationString();
            if (situation.Contains("Landed") || situation.Contains("flight"))
                return FlightGlobals.currentMainBody.BiomeMap.GetAtt(vessel.latitude * Mathf.Deg2Rad, vessel.longitude * Mathf.Deg2Rad).name + situation;


            return situation;
        }
        public double getResourceBudget(string name)
        {
            //   
            if (this.vessel == null) return 0;
           // if (this.vessel == FlightGlobals.ActiveVessel)
            {
                // //print("found vessel event!");
                var resources = vessel.GetActiveResources();
                for (int i = 0; i < resources.Count; i++)
                {
                  //  print("vessel has resources!");
                   // print(resources[i].info.name);
                   // print("im looking for " + name);
                    if (resources[i].info.name == name)
                    {
                        // //print("Found the resouce!!");
                        return resources[i].amount;

                    }
                }
            }
            return 0;
        }
        public double getResourceBudget(int name)
        {
            //   
            if (this.vessel == null) return 0;
            // if (this.vessel == FlightGlobals.ActiveVessel)
            {
                // //print("found vessel event!");
                var resources = vessel.GetActiveResources();
                for (int i = 0; i < resources.Count; i++)
                {
                    //  print("vessel has resources!");
                    // print(resources[i].info.name);
                    // print("im looking for " + name);
                    if (resources[i].info.id == name)
                    {
                        // //print("Found the resouce!!");
                        return resources[i].amount;

                    }
                }
            }
            return 0;
        }
        public double getResourceCap(string name)
        {
            if (this.vessel == null) return 0;
            //   
            if (this.vessel == FlightGlobals.ActiveVessel)
            {
                // //print("found vessel event!");
                var resources = vessel.GetActiveResources();
                for (int i = 0; i < resources.Count; i++)
                {
                    // //print("vessel has resources!");
                    ////print(resources[i].info.name);
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
        public double getResourceCap(int name)
        {
            if (this.vessel == null) return 0;
            //   
            if (this.vessel == FlightGlobals.ActiveVessel)
            {
                // //print("found vessel event!");
                var resources = vessel.GetActiveResources();
                for (int i = 0; i < resources.Count; i++)
                {
                    // //print("vessel has resources!");
                    ////print(resources[i].info.name);
                    // //print("im looking for " + resourceName);
                    if (resources[i].info.id == name)
                    {
                        // //print("Found the resouce!!");
                        return (double)resources[i].maxAmount;

                    }
                }
            }
            return 0;
        }
        bool vesselHasEnoughResource(string name, float rc)
        {
            //   
            
            if (this.vessel == FlightGlobals.ActiveVessel)
            {
                ////print("found vessel event!");
                var resources = vessel.GetActiveResources();
                for (int i = 0; i < resources.Count; i++)
                {
                    ////print("vessel has resources!");
                    ////print(resources[i].info.name);
                    ////print("im looking for " + resourceName);
                    if (resources[i].info.name == resourceName)
                    {
                        ////print("Found the resouce!!");
                        if (resources[i].amount >= resourceAmount)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }




    }
    public class MovieTheater: BaseConverter
    {
        [KSPField(isPersistant = false, guiActive = false)]
        public float pilotBonus = .10f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float engineerBonus = .10f;
        [KSPField(isPersistant = false, guiActive = false)]
        public float scientistBonus = .10f;

        [KSPField(isPersistant = false, guiActive = false)]
        public float loveMovieBonus = .50f;

        [KSPField(isPersistant = false, guiActive = false)]
        public string InspirationResourceName = "inspiration";

        [KSPField(isPersistant = true, guiActive = true, guiName="Movie Type Playing")]
        public string MovieType="none";
        [KSPField(isPersistant = true, guiActive = true, guiName = "Movie Type Playing")]
        public string MovieBonus = "none";

        void resetPartInspiration()
        {
            //float currentInspiration = getResourceBudget(generatorResourceOutName);

            this.part.RequestResource(InspirationResourceName, 99999999, ResourceFlowMode.NO_FLOW); //reset this part's resource only

        }
        #region regolith
        protected override float GetHeatMultiplier(ConverterResults result, double deltaTime)
        {
            return base.GetHeatMultiplier(result, deltaTime);
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
             MovieBonus = "10% reduced Engineer recruitment cost";
             StartResourceConverter();
             Events["StartResourceConverter"].active = false;
             Events["StopResourceConverter"].active = false;

         }
         [KSPEvent(guiName = "Play Scifi Movies (pilot bonus)", active = true, guiActive = true)]
         public void playScifi()
         {
             resetPartInspiration();
             MovieType = "Scifi Movies";
             MovieBonus = "10% reduced Pilot recruitment cost";
             StartResourceConverter();
             Events["StartResourceConverter"].active = false;
             Events["StopResourceConverter"].active = false;

         }
         [KSPEvent(guiName = "Play Documentaries (scientist bonus)", active = true, guiActive = true)]
         public void playDocumentaries()
         {
             resetPartInspiration();
             MovieType = "Documentaries";
             MovieBonus = "10% reduced Scientist recruitment cost";
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



    public class CivilianPopulationRegulator :  BaseConverter
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

        [KSPField(isPersistant = false, guiActive = true)]
        public float kerbalMass = 105.0f;

        [KSPField(isPersistant = false, guiActive = true)]
        public float foodPerPop;  //this is how much food is needed for the population to grow, and how much is generated when the population shrinks
        //this is calculated now, based on kerbal mass.
        //new civie pop can be recruited at this rate of food per population.  open resource system has a food definition.  That wil lbe fine

        [KSPField(isPersistant = true, guiActive = true, guiName = "Rent")]
        public float taxes = 100;

        [KSPField(isPersistant = true, guiActive = true, guiName = "Time until next pay")]
        public float TimeUntilTaxes = 21600;

        [KSPField(isPersistant = false, guiActive = false)]
        public string consumptionSpeed;  //this is how long it takes for a civilian kerbal to consume 1kg of each resource


        [KSPField(isPersistant = false, guiActive = false)]
        public string populationResourceName;

        [KSPField(isPersistant = false, guiActive = true)]
        public float populationDecayRate;  //how fast does the civie population jump ship when there's no food?

        [KSPField(isPersistant = false, guiActive = true)]
        public float populationGrowthRate;  //how fast does the civie population go to ship when there's food?
        [KSPField(isPersistant = false, guiActive = true)]
        public float reproductionRate;

        [KSPField(isPersistant = true, guiActive = true)]
        public float growthRate;  //show the player how much food their population is consuming


        [KSPField(isPersistant = true, guiActive = true)]
        public float populationGrowthTimer;  //how fast does the civie population jump ship when there's no food?

        [KSPField(isPersistant = true, guiActive = true)]
        public float populationDecayTimer;  //how fast does the civie population jump ship when there's no food?

        [KSPField(isPersistant = true, guiActive = true)]
        public float CostToReproduce;  //show the player how much food their population is consuming

        [KSPField(isPersistant = true, guiActive = true)]
        public bool civilianDock;

        [KSPField(isPersistant = true, guiActive = true)]
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
            
            if(!UsePT)
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
                    return civilianDockGrowthRate / 100;
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

        Dictionary<string, double> needs;
        Dictionary<string, double> wastes;
        double dConsumptionRate;
        Dictionary<string, double> resourceDensity;
        Dictionary<string, string> wasteProducts;
       
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
            if(master || slave)
            {
                base.OnStart(state);
                return;
            }
            
            master = true;
            var partsWithCivies = vessel.FindPartModulesImplementing<CivilianPopulationRegulator>();
            foreach(CivilianPopulationRegulator p in partsWithCivies)
            {
                if(p.master)
                    continue;
                p.slave = true; 
                if(p.civilianDock)
                {
                    civilianDock = p.civilianDock;
                    civilianDockGrowthRate = p.civilianDockGrowthRate;
                }

            }
            StartResourceConverter();
          

            base.OnStart(state);
        }
        void simulateFarms(double dt)
        {
            var farmlist = vessel.FindPartModulesImplementing<FarmGenerator>();
            foreach (FarmGenerator farm in farmlist)
            {
               // print(farm.generatorActivateName + " " + farm.generatorActive.ToString());
                //if(farm.generatorActive)
                    farm.runFarm2(dt, true);

            }
        }

        bool getTheaterBonus()
        {
            var theaters = vessel.FindPartModulesImplementing<MovieTheater>();
            foreach (MovieTheater t in theaters)
                if (t.MovieType == "Love Movies")
                    return true;
            return false;
        }
        #region regolith
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
            //up to 50 people the rent is 100 per

            double y = x;
            double rent = 100;
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
            double foodbudget =  base.ResBroker.AmountAvailable(this.part,foodResourceName, dt, "all");//getResourceBudget(foodResourceName);
           // PartResourceDefinition def = new PartResourceDefinition(foodResourceName);
           // print("1");
            //mass = unit * density
            //mass / density = unit
            double foodRequired = kerbalMass / 0.28102905982906; //lol density of food in kg
            double currentPop = base.ResBroker.AmountAvailable(this.part, populationResourceName, dt, "all");//getResourceBudget(populationResourceName);
            double co2 = base.ResBroker.AmountAvailable(this.part, "CarbonDioxide", dt, "all");
            double co2Cap = base.ResBroker.StorageAvailable(this.part, "CarbonDioxide", dt, "all", 0);
            
            growthRate = (float)(currentPop / reproductionRate);
            if (getTheaterBonus())
                growthRate += growthRate * .5f;
            if (growthRate < 1)  //can't grow population unless it's big enough
                growthRate = 0;
           // print("2");
            if (civilianDock)
            {
                foodRequired = 0; //new kerbals arriving don't need to spend food to grow
                growthRate = getRecruitmentRate(); //kerbal recruitment much faster than reproduction
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
            //print("3a");
            //no air
           // needsMet = needsMet && (co2 != co2Cap);
            //if (co2 == co2Cap)
             //   decayMultiplier = 10; //decay faster with no air
          
            if (needsMet)
            {
              //  print("3b");
                populationGrowthTimer += (float)dt * (float)growthRate;
                populationDecayTimer = 0;
                TimeUntilTaxes -= (float)dt;
                float taxesAcquired =  (float)calculateRent(currentPop); //100 funds per civilian
                taxes = taxesAcquired;
              //  print("3c");
                if(TimeUntilTaxes < 0)
                {
                    
                    //CurrencyModifierQuery q = CurrencyModifierQuery.RunQuery(TransactionReasons.ContractReward, taxesAcquired, 0, 0);
                   // q.AddDelta(Currency.Funds, taxesAcquired);
                    Funding.Instance.AddFunds(taxes, TransactionReasons.Vessels);
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
                if(popMade < 0)
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
           // print("6");
            //spend the food required
            //they're not wolfing down all the food now are they

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
       // public override void OnUpdate()
        {
           // System.Diagnostics.Trace.WriteLine("civie pop dt a " + master.ToString() + " c" + slave.ToString());
            //base.OnFixedUpdate();
            //var recipe = base.Recipe;
           // recipe.ToString();
            print("Update!");
           // base.
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
          //use regolith to calcualte the resource change
           // double dt = calculateRateOverTime();
            //print("DT is " + dt.ToString());


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
        public string inspirationResourceName;
        [KSPField(isPersistant = false, guiActive = false)]
        public float inspirationCost;

       
        public static void screenMessage(string msg)
        {
            ScreenMessage smg = new ScreenMessage(msg, 4.0f, ScreenMessageStyle.UPPER_CENTER);
            ScreenMessages.PostScreenMessage(smg);
        }

        double applyTheaterBonus(double cost, KerbalJob job)
        {
            var theaters = vessel.FindPartModulesImplementing<MovieTheater>();
            Debug.Log(theaters.Count);
           

            foreach(MovieTheater t in theaters)
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
            switch (job)
            {
                case KerbalJob.Pilot:
                    KerbalRoster.SetExperienceTrait(newMember, "Pilot");
                    break;
                case KerbalJob.Engineer:
                    KerbalRoster.SetExperienceTrait(newMember, "Engineer");
                    break;
                case KerbalJob.Scientist:
                    KerbalRoster.SetExperienceTrait(newMember, "Scientist");
                    break;
            }

            float startXP = KerbalRoster.GetExperienceLevelRequirement(startLevel);

            newMember.experience = startXP + 1;
            newMember.experienceLevel = KerbalRoster.CalculateExperienceLevel(newMember.experience);
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
           
            if(vessel.GetCrewCount() < vessel.GetCrewCapacity())
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
            
            float budget = getResourceBudget(populationName);
            if (budget < civilianPopulationCost)
            {
                screenMessage("No civilians to recruit");
                return;
            }
           
            int xplevel = 1;
            //if this is a flight school recruitment requires 5000 flight experience
            double flightXP = getResourceBudget(flightExperienceResourceName);
            double tempFlightXPCost = applyTheaterBonus(flightExperienceCost, KerbalJob.Pilot);
            if(flightschool)
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
            double tempInspirationCost =  applyTheaterBonus(inspirationCost, KerbalJob.Pilot);

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
                    if(flightschool)
                        part.RequestResource(flightExperienceResourceName, tempFlightXPCost);
                }

            }



        }
        [KSPEvent(guiName = "Recruit Engineer", active = true, guiActive = true)]
        public void RecruitEngineerKerbal()
        {

            float budget = getResourceBudget(populationName);
            if (budget < civilianPopulationCost)
            {
                screenMessage("No civilians to recruit");
                return;
            }
            
            int xplevel = 1;
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
                    if(university)
                    part.RequestResource(educationResourceName, tempEducationCost);
                    part.RequestResource(inspirationResourceName, tempInspirationCost);
                }

            }



        }
        [KSPEvent(guiName = "Recruit Scientist", active = true, guiActive = true)]
        public void RecruitScienceKerbal()
        {

            float budget = getResourceBudget(populationName);
            if (budget < civilianPopulationCost)
            {
                screenMessage("No civilians to recruit");
                return;
            }
           
            int xplevel = 1;
           
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
                    if(university)
                        part.RequestResource(educationResourceName, tempEducationCost);
                    part.RequestResource(inspirationResourceName, tempInspirationCost);
                }

            }



        }

        public override void OnStart(PartModule.StartState state)
        {
            if (allowEngineerScientist ||allowPilot )
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
   


    [KSPAddon(KSPAddon.Startup.SpaceCentre| KSPAddon.Startup.Flight, false)]
    public class AddScenarioModules : MonoBehaviour
    {
        void Start()
        {
            var game = HighLogic.CurrentGame;
            print("Try to add module");
            var psm = game.scenarios.Find(s => s.moduleName == typeof(PipeLineNetworkManager).Name);
            if (psm == null)
            {
                print("adding module!");
                game.AddProtoScenarioModule(typeof(PipeLineNetworkManager),
                    GameScenes.SPACECENTER,
                    GameScenes.FLIGHT, GameScenes.EDITOR);
            }
            else
            {
                if (psm.targetScenes.IndexOf(GameScenes.FLIGHT) <0)
                {
                    print("adding module!");
                    psm.targetScenes.Add(GameScenes.FLIGHT);
                }
                if (psm.targetScenes.IndexOf(GameScenes.SPACECENTER) < 0)
                {
                    print("adding module!");
                    psm.targetScenes.Add(GameScenes.SPACECENTER);

                }
                if (psm.targetScenes.IndexOf(GameScenes.EDITOR) < 0)
                {
                    print("adding module!");
                    psm.targetScenes.Add(GameScenes.EDITOR);
                }
            }
        }
        
    }
   
    public class pipelineShip
    {
        public string id;
        public string name;
        public string networkID = string.Empty;
        public string body;
        public double lat;
        public double lon;
        public double altitude;
        public bool loaded;
        public Dictionary<string, double> resourceSupply;
        public Dictionary<string, double> resourceDemand;
        public Dictionary<string, double> resourceCapacity;
        public Dictionary<string, double> networkDraw;
        //regolith will duplicate the resource demand while we were away
        //to prevent that, record what was produced while away by our resourceDemand
        //then we reverse it with our resource demand in the update function
        //resource demand is in units per second!
        public Dictionary<string, double> unfocusedProductivity;
        public bool needsResourceUpdate = false;
        public void debug()
        {
           MonoBehaviour.print( "ship id;" + id.ToString());
           MonoBehaviour.print("ship name:" + name);
         MonoBehaviour.print( "Guid networkID = Guid.Empty;" + networkID.ToString());
         MonoBehaviour.print( "string body;" + body);

         MonoBehaviour.print(" double lat;" + lat.ToString());
         MonoBehaviour.print(" double lon;" + lon.ToString());
         MonoBehaviour.print("double altitude;" + altitude.ToString());

         xPipeLine.debugDictionary("resource supply", resourceSupply);
         xPipeLine.debugDictionary("resourceDemand ", resourceDemand);
         xPipeLine.debugDictionary("resourceCapacity ", resourceCapacity);
         xPipeLine.debugDictionary("networkDraw ", networkDraw);
         xPipeLine.debugDictionary("unfocusedProductivity", unfocusedProductivity);
            try
             {
                 foreach (string key in resourceSupply.Keys)
                 {
                     MonoBehaviour.print(key + " " + resourceSupply[key].ToString() + " " + resourceDemand[key].ToString() );
                 }
             }
             catch { }
        }

        public static Dictionary<string, double> DictionaryFromString(string dic)
        {
            Dictionary<string, double> ret = new Dictionary<string,double>();
            var dicTokens = dic.Split(';');
            foreach(string t in dicTokens)
            {
                var tt = t.Split(',');
                ret[tt[0]] = double.Parse(tt[1]);
            }
            return ret;
        }

        public static string StringFromDictionary(Dictionary<string, double> dic)
        {
            string ret = "";
            foreach (string k in dic.Keys)
                ret += k + "," + dic[k].ToString() + ";";
            return ret.TrimEnd(';');
        }
        public static pipelineShip loadConfig(ConfigNode node)
        {
            try
            {
                pipelineShip ret = new pipelineShip();

                ret.id = (node.GetValue("id"));
                ret.body = node.GetValue("body");
                ret.name = node.GetValue("name");
                ret.lat = double.Parse(node.GetValue("lat"));
                ret.lon = double.Parse(node.GetValue("lon"));
                ret.altitude = double.Parse(node.GetValue("altitude"));
                ret.resourceSupply = DictionaryFromString(node.GetValue("resourceSupply"));
                ret.resourceDemand = DictionaryFromString(node.GetValue("resourceDemand"));
                ret.resourceCapacity = DictionaryFromString(node.GetValue("resourceCapacity"));
                ret.networkDraw = DictionaryFromString(node.GetValue("networkDraw"));

                ret.unfocusedProductivity = DictionaryFromString(node.GetValue("unfocusedProductivity"));
                return ret;
            }
            catch { return null; }

        }
        public static pipelineShip fromVessel(Vessel v, ref xPipeLine pipe)
        {
            pipelineShip ret = new pipelineShip();

            ret.id = v.id.ToString();
            ret.lat = v.latitude;
            ret.lon = v.longitude;
            ret.name = v.RevealName() ;
            ret.body = v.mainBody.name;
            ret.altitude = v.altitude;
            var modulelist =  v.FindPartModulesImplementing<xPipeLine>();
            ret.resourceSupply = new Dictionary<string, double>();
            ret.resourceDemand = new Dictionary<string, double>();
            ret.resourceCapacity = new Dictionary<string, double>();
            ret.unfocusedProductivity = new Dictionary<string, double>();
            ret.loaded = true;
            foreach(string res in pipe.totalSupply.Keys)
            {
                ret.resourceSupply[res] = pipe.totalSupply[res];
                ret.resourceDemand[res] = pipe.totalDemand[res];
                ret.resourceCapacity[res] = pipe.totalCapacity[res];
                ret.unfocusedProductivity[res] = 0;
            }
            ret.networkDraw = new Dictionary<string,double>();
            foreach(string res in pipe.networkDelta.Keys)
                ret.networkDraw[res] = pipe.networkDelta[res];
           
            return ret;
        }
        public  void updateVessel(Vessel v, ref xPipeLine pipe)
        {
            

            id = v.id.ToString();
            lat = v.latitude;
            name = v.RevealName();
            lon = v.longitude;
            body = v.mainBody.name;
            altitude = v.altitude;
            //var modulelist = v.FindPartModulesImplementing<xPipeLine>();
            foreach (string res in pipe.totalSupply.Keys)
            {
                resourceSupply[res] = pipe.totalSupply[res];
                resourceDemand[res] = pipe.totalDemand[res];
                resourceCapacity[res] = pipe.totalCapacity[res];  //don't touch the unfocused productivity
            }
            foreach(string res in pipe.networkDelta.Keys)
                networkDraw[res] = pipe.networkDelta[res];
            loaded = v.loaded;

            debug();
           // resourceSupply = new Dictionary<string, double>();
          //  resourceDemand = new Dictionary<string, double>();
           // ret.resourceCapacity = new Dictionary<string, double>();
            
        }
        public void saveConfig(ConfigNode node)
        {
            debug();
            ConfigNode shipConfig = node.AddNode("ship");
            shipConfig.AddValue("id", id);
            shipConfig.AddValue("lat", lat);
            shipConfig.AddValue("lon", lon);
            shipConfig.AddValue("name", name);
            shipConfig.AddValue("altitude", altitude);
            shipConfig.AddValue("resourceSupply", StringFromDictionary(resourceSupply));
            shipConfig.AddValue("resourceDemand", StringFromDictionary(resourceDemand));
            shipConfig.AddValue("resourceCapacity", StringFromDictionary(resourceCapacity));
            shipConfig.AddValue("networkDraw", StringFromDictionary(networkDraw));
            shipConfig.AddValue("unfocusedProductivity", StringFromDictionary(unfocusedProductivity));
            
        }


    }

   
    public class PipeLineNetworkManager: ScenarioModule
    {
        double maxNetworkRange = 10000;
        public static PipeLineNetworkManager instance;

        Dictionary<string, pipelineShip> ships;
        Dictionary<string, List<pipelineShip>> Networks;
        
        public PipeLineNetworkManager()
        {
            instance = this;
            ships = new Dictionary<string, pipelineShip>();
            Networks = new Dictionary<string, List<pipelineShip>>();
        }

        bool networkContainsShip(string id, pipelineShip p, double range)
        {
            Vector3d pLoc = FlightGlobals.currentMainBody.GetWorldSurfacePosition(p.lat, p.lon, p.altitude);
            foreach(pipelineShip xp in Networks[id])
            {
                Vector3d xploc = FlightGlobals.currentMainBody.GetWorldSurfacePosition(xp.lat, xp.lon, xp.altitude);
                if (Vector3d.Distance(xploc, pLoc) < range)
                    return true;

            }
            return false;
        }

        List<string> networksInRange(pipelineShip p, double range)
        {
            List<string> ret = new List<string>();
            foreach (string id in Networks.Keys)
            {
                if (networkContainsShip(id, p, range))
                    ret.Add(id);
            }
            return ret;
        }
        public void mergeNetworks(string a, string b)
        {
            foreach(pipelineShip xp in Networks[b])
            {
                Networks[a].Add(xp);
                xp.networkID = a;

            }
            Networks.Remove(b);

        }
        public void addShipTonetwork(pipelineShip p, string id)
        {
            Networks[id].Add(p);
            p.networkID = id;
        }

        public void updateNetworks()
        {
            //to avoid lots and lots of calculations, we'll connect all ships on the same body to the same network if they can be connected
            print("begin build networks");
            Networks = new Dictionary<string, List<pipelineShip>>();

            List<string> ghostShips = new List<string>();
            foreach(string shipid in ships.Keys)
            {
                if (FlightGlobals.Vessels.Find(s => s.id.ToString() == shipid) == null)
                {
                    ghostShips.Add(shipid);

                    
                }
            }
            foreach (string s in ghostShips)
                ships.Remove(s);


            foreach (string shipid in ships.Keys)
            {
               
                //for each ship, determine if it can be part of a network.
                var foundNetworks = networksInRange(ships[shipid], maxNetworkRange);
                while (foundNetworks.Count > 1)
                {
                    mergeNetworks(foundNetworks[0], foundNetworks[1]);
                }
                if (foundNetworks.Count > 0)
                    addShipTonetwork(ships[shipid], foundNetworks[0]);
                if(foundNetworks.Count == 0)
                {
                    string newNetwork = Guid.NewGuid().ToString();
                    Networks.Add(newNetwork, new List<pipelineShip>());
                    addShipTonetwork(ships[shipid], newNetwork);

                }
                ships[shipid].needsResourceUpdate = true;  

            }
            debugNetworks();

        }
        void debugNetworks()
        {
            print("Networks! " + Networks.Keys.Count.ToString());
            foreach (string networkID in Networks.Keys)
            {
                print("NetworkID: " + networkID.ToString());
                foreach (pipelineShip ship in Networks[networkID])
                    ship.debug();


            }
        }
        public void updateNetworkResources(double dt)
        {
            bool wanUpdate = false;
            foreach(var ship in ships.Keys)
            {
                if (ships[ship].loaded)
                    {
                      
                        continue; //ship is updating in realtime, don't process it
                    }
                wanUpdate = true;
                try
                {
                    foreach (var res in ships[ship].resourceDemand.Keys)
                    {

                        ships[ship].resourceSupply[res] -= ships[ship].resourceDemand[res] * dt;
                        if (ships[ship].unfocusedProductivity.ContainsKey(res))
                            ships[ship].unfocusedProductivity[res] -= ships[ship].resourceDemand[res] * dt;
                        else
                            ships[ship].unfocusedProductivity[res] = 0;
                        if (ships[ship].resourceSupply[res] > ships[ship].resourceCapacity[res])
                            ships[ship].resourceSupply[res] = ships[ship].resourceCapacity[res];
                        if (ships[ship].resourceSupply[res] < 0)
                            ships[ship].resourceSupply[res] = 0;


                    }
                }
                catch ( Exception e)
                {
                    print(e.InnerException.ToString());
                }

                

            }
           // print("Wide area update? " + wanUpdate.ToString());
            //step 2 apply network draw evenly  to ships with each resource

            foreach (var ship in ships.Keys)
            {
                if (ships[ship].loaded)
                {

                    continue; //ship is updating in realtime, don't process it
                }
                foreach (var res in ships[ship].networkDraw.Keys)
                {
                    double consumed = requestNetworkResource(ships[ship], res, ships[ship].networkDraw[res]*dt);

                }
            }
        
           
        
        }
        public int shipsWithResource(List<pipelineShip> network, string resource, pipelineShip me, bool ignore)
        {
            int count = 0;
            foreach(var ship in network)
                if(ship.resourceSupply.ContainsKey(resource) && (ignore && ship.id != me.id))
                    count++;
            return count;
        }
        public double networkResourceAvailable(Vessel v, string resource)
        {
            try{
                pipelineShip p = ships[v.id.ToString()];
                double resourceAvailable = 0;
                foreach (pipelineShip px in Networks[p.networkID])
                {
                    if (px.id == p.id) continue;
                    if (px.loaded) //lan ship.. we can draw resource directly from it
                    {
                        Vessel loaded = FlightGlobals.Vessels.Find(x => x.id.ToString() == px.id);
                        if(loaded != null)
                        {
                            var vesselResources = loaded.GetActiveResources();
                            foreach (var res in vesselResources)
                                if (res.info.name == resource)
                                    resourceAvailable += res.amount;
                        }
                    }
                    else
                    {

                        if (px.resourceCapacity.ContainsKey(resource))
                            resourceAvailable += px.resourceSupply[resource];
                    }
                }
                return resourceAvailable;
            }
            catch {return 0;}  //ship isn't in a network, return 0

        }
         public double requestNetworkResource(Vessel v, string resource, double amount)
        {
            try
            {
                pipelineShip p = ships[v.id.ToString()];
                return requestNetworkResource(p, resource, amount);
               
            }
             catch
            {
                 return 0;
             }

        }
        public double requestNetworkResource(pipelineShip v, string resource, double amount)
        {
            try
            {
                pipelineShip p = v;
                double count =shipsWithResource(Networks[p.networkID], resource, p, true);
                if (count == 0) return 0;
                double totalRequested = 0;
                double requestPerShip = amount / count;
                //print("count = " + count.ToString());
              //  print("amount = " + amount.ToString());
              //  print("requestPerShip = " + requestPerShip.ToString());
                foreach (pipelineShip px in Networks[p.networkID])
                {
                    if (px.id == p.id) continue;
                    
                    if (px.loaded) //lan ship.. we can draw resource directly from it
                    {
                        //print("lan resource draw pxloaded=" + px.loaded.ToString() );
                       
                        Vessel loaded = FlightGlobals.Vessels.Find(x => x.id.ToString() == px.id);
                       // print("found vessel? " + (loaded == null).ToString());
                        if (loaded != null)
                        {
                            px.loaded = loaded.loaded;
                            var vesselResources = loaded.GetActiveResources();
                            foreach (var res in vesselResources)

                                if (res.info.name == resource)
                                {
                                    if (requestPerShip > 0) //drawing resource
                                    {
                                        //if supply is 1 and request is 2, return 1
                                        double thisShipSupplied = Math.Min(res.amount, requestPerShip);
                                        totalRequested += thisShipSupplied;
                                        double consumed = loaded.parts[0].RequestResource(res.info.name, thisShipSupplied);
                                        var loadedPipeline = loaded.FindPartModulesImplementing<xPipeLine>();
                                        for (int i = 0; i < loadedPipeline.Count; i++)
                                            if (loadedPipeline[i].localMaster)
                                            {
                                                loadedPipeline[i].setConsumed(res.info.name,consumed);
                                            }
                                        
                                    }
                                    if (requestPerShip < 0) //drawing resource
                                    {
                                        //if supply is 1 away from cap and request is 2, return 1
                                        double thisShipSupplied = Math.Min(res.maxAmount - res.amount, -requestPerShip);
                                        totalRequested -= thisShipSupplied;
                                       double consumed =  loaded.parts[0].RequestResource(res.info.name, -thisShipSupplied);
                                       var loadedPipeline = loaded.FindPartModulesImplementing<xPipeLine>();
                                       for (int i = 0; i < loadedPipeline.Count; i++)
                                           if (loadedPipeline[i].localMaster)
                                               loadedPipeline[i].setConsumed(res.info.name, consumed); //we got here... there's a pipeline on the ship
                                    }
                                    break;
                                }
                        }
                    }
                    else
                    {
                        if (px.resourceCapacity.ContainsKey(resource))
                        {
                            if (requestPerShip > 0) //drawing resource
                            {
                                //if supply is 1 and request is 2, return 1
                                double thisShipSupplied = Math.Min(px.resourceSupply[resource], requestPerShip);
                                totalRequested += thisShipSupplied;
                                px.resourceSupply[resource] -= thisShipSupplied;
                            }
                            if (requestPerShip < 0) //drawing resource
                            {
                                //if supply is 1 away from cap and request is 2, return 1
                                double thisShipSupplied = Math.Min(px.resourceCapacity[resource] - px.resourceSupply[resource], -requestPerShip);
                                totalRequested -= thisShipSupplied;
                                px.resourceSupply[resource] += thisShipSupplied;
                            }
                            //if request is 0, do nothing
                        }
                    }

                }
                return totalRequested;
            }
            catch { return 0; }  //ship isn't in a network, return 0
        
        }

        public void applyNetworkDemand(double dt, pipelineShip p)
        {
           // double demandPerShip = dt / Networks[p.networkID].Count;
            //demand is in time!
            foreach (pipelineShip px in Networks[p.networkID])
            {
                foreach(string k in px.resourceCapacity.Keys)
                {
                    px.resourceCapacity[k] -= dt;
                }
            }
        }
        public void getNetworkResources(pipelineShip p, ref xPipeLine pipeModule, double dt)
        {
            Dictionary<string, double> supply = new Dictionary<string, double>();
            Dictionary<string, double> demand = new Dictionary<string, double>();
            Dictionary<string, double> capacity = new Dictionary<string, double>();
           // pipeModule.contributors = new Dictionary<string, Dictionary<string, double>>();

            foreach(pipelineShip px in Networks[p.networkID])
           {
               if (px.id == p.id) continue;
                foreach(string k in px.resourceSupply.Keys)
                {
                    if (k == "pipeNetwork")
                        continue;
                    if (supply.ContainsKey(k))
                    {
                        double capacityScalar = Math.Min(px.resourceCapacity[k], dt) / dt;
                        supply[k] += px.resourceSupply[k] * capacityScalar;
                        demand[k] += px.resourceDemand[k];
                        
                    }
                    else
                    {
                        double capacityScalar = Math.Min(px.resourceCapacity[k], dt) / dt;
                        supply[k] = px.resourceSupply[k] * capacityScalar;
                        demand[k] = px.resourceDemand[k];
                         
                    }
                  

                }
                
              

           }
            //pipeModule.part.Resources.list.Clear();
            //foreach (string k in amounts.Keys)
            //{
            //    ConfigNode resourceNode = new ConfigNode("RESOURCE");

            //    resourceNode.AddValue("name", k);
            //    resourceNode.AddValue("amount", amounts[k].ToString());
            //    resourceNode.AddValue("maxAmount", maximums[k].ToString());
            //    print("try to add resource...");
            //    try
            //    {
            //        var resource = pipeModule.part.AddResource(resourceNode);

            //        print("resource defined " + resource.info.name + " " + resource.amount.ToString() + " " + resource.maxAmount.ToString());
            //    }
            //    catch { }

            //}

           print("part has resources " + pipeModule.part.Resources.Count.ToString());
           foreach (var res in pipeModule.part.Resources.list)
               print("resource " + res.info.name + " " + res.amount.ToString() + " " + res.maxAmount.ToString());

           string recipeOutputs = "";
           foreach (string k in demand.Keys)
           {
               
               recipeOutputs += k + "," + ( (supply[k] - demand[k])).ToString() + ",true;";
           }
            recipeOutputs = recipeOutputs.TrimEnd(';');
            print("recipe outputs = " + recipeOutputs);

            pipeModule.RecipeOutputs = recipeOutputs;
            pipeModule.reloadRecipe();
            print("adjusting mass... resource mass = " + pipeModule.part.GetResourceMass().ToString());
           
            print("part mass..." + pipeModule.part.mass.ToString());

            p.needsResourceUpdate = false;
        }
        public void clearResources(ref xPipeLine pipeModule)
        {

          //  pipeModule.part.Resources.list.Clear();
            string recipeOutputs = "";
            
            pipeModule.RecipeOutputs = recipeOutputs;
            pipeModule.reloadRecipe();
           

        }
        public void setNetworkResources(pipelineShip v, ref xPipeLine pipeModule, double dt)
        {
            
            
            
           // foreach (var res in pipeModule.part.Resources.list)

            foreach(string res in pipeModule.totalDemand.Keys)
            {
                v.resourceDemand[res] = pipeModule.totalDemand[res];
            }
            foreach (string res in pipeModule.totalSupply.Keys)
            {
                v.resourceSupply[res] = pipeModule.totalSupply[res];
            }
                //double capacityScalar = res.amount;
                //print("addResource:" + capacityScalar.ToString());
                //foreach (pipelineShip px in Networks[v.networkID])
                //     if (px.resourceMaximums.ContainsKey(res.info.name) && px.id != v.id)
                //    {
                //        double deltaRes = px.resourceMaximums[res.info.name] - px.resourceAmounts[res.info.name];
                //         deltaRes = Math.Min(capacityScalar, deltaRes);
                //         px.resourceAmounts[res.info.name] += deltaRes; //distribute network resources evenly
                //      capacityScalar -= deltaRes;
                //      print("added " + deltaRes + " to go " + capacityScalar);

                //    }
                   
            //    if(v.resourceSupply.ContainsKey(res.name))
            //    {
            //        v.resourceDeltas[res.info.name] = (res.amount - v.resourceAmounts[res.info.name]) * dt;
            //        v.resourceAmounts[res.info.name] = res.amount;
                    
            //    }
            //}
            
           

        }
        int recalc = 0;
        public void unfocusedProducitivty(xPipeLine pipeModule)
        {
            string id = pipeModule.vessel.id.ToString();
            if (ships.ContainsKey(id))
            {

                foreach (string res in ships[id].resourceSupply.Keys)
                {
                    if (ships[id].unfocusedProductivity.ContainsKey(res))
                    {
                        if (ships[id].unfocusedProductivity[res] != 0)
                        {
                            pipeModule.part.RequestResource(res, 999999999999);
                            pipeModule.part.RequestResource(res, -ships[id].resourceSupply[res]);
                            ships[id].unfocusedProductivity[res] = 0;
                        }
                    }
                }

            }
        }
        public void resetShipResources(xPipeLine pipeModule)
        {
            string id = pipeModule.vessel.id.ToString();
            if (ships.ContainsKey(id))
            {
               
                foreach(string res in ships[id].resourceSupply.Keys)
                {
                    pipeModule.part.RequestResource(res, 999999999999);
                    pipeModule.part.RequestResource(res, -ships[id].resourceSupply[res]);
                }

            }
        }
        public void setShipLoaded(xPipeLine pipeModule, bool loaded)
        {
            string id = pipeModule.vessel.id.ToString();
            if (ships.ContainsKey(id))
            {

                ships[id].loaded = loaded;

            }
        }
        public void addShipToBodyNetwork(Vessel v, xPipeLine pipeModule, bool resetVesselResources, double dt)
        {
            if (ships.ContainsKey(v.id.ToString()))
            {
                ships[v.id.ToString()].updateVessel(v, ref pipeModule);
                
            }
            else
            {
                ships[v.id.ToString()] = pipelineShip.fromVessel(v, ref pipeModule);
                updateNetworks(); //only update whole network list during add or remove
                print("new ship, add resources.");
               // getNetworkResources(ships[v.id.ToString()], ref pipeModule, dt);
            }
        }
       
      
        public void removeShipFromBodyNetwork(Vessel v, xPipeLine pipeModule)
        {
            if (ships.ContainsKey(v.id.ToString()))
            {
                ships.Remove(v.id.ToString());
            }
            updateNetworks();
            clearResources(ref pipeModule);
        }

       


        public override void OnLoad(ConfigNode basenode)
        {
            base.OnLoad(basenode);
            instance = this;
            print("Loading pipeline network manager!");
            var shiplist = basenode.GetNodes("ship");
            foreach(var shipNode in shiplist)
            {
                var loadship = pipelineShip.loadConfig(shipNode);
                if(loadship != null)
                    ships[loadship.id] = loadship;

            }
            updateNetworks();
           
        }
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            print("Saving pipeline network manager!");
            foreach (string ship in ships.Keys)
                ships[ship].saveConfig(node);
          
        }

       


    }

    public class ContributedResource
    {
        
        public string name;

    }
    #region implementaiton2
    public class dbShipResources
    {
        public Dictionary<string, double> amounts;
        public Dictionary<string, double> capcity;
        public Dictionary<string, double> deltas;
        public dbShipResources()
        {
            amounts = new Dictionary<string,double>();
            capcity = new Dictionary<string,double>();
            deltas = new Dictionary<string,double>();
        }
    }
    public class xPipeLine2 : BaseConverter
     {
        public static Vessel[] myNetwork;

        [KSPField(isPersistant = true)]
        public bool Connected;
        [KSPField(isPersistant = true)]
        public bool connectionInProgress = false;

        [KSPField(isPersistant = true, guiActive= false)]
        public float buildCost;
        [KSPField]
        public float costPerMeter;

        [KSPField]
        public float breakDuration = 3;
        double pipeBreakTimer = 0;
        //build is 1 meter per second

        [KSPField]
        public string RecipeInputs = "";

        [KSPField]
        public string RecipeOutputs = "";

        [KSPField]
        public string RequiredResources = "";

        float calculateBuildCost()
        {
            double dist = distanceToClosestVessel();
            return (float)dist * costPerMeter;
        }
        [KSPField(isPersistant = true)]
        public string connectionSpot;
        void loadNetwork()
        {
            xPipeLine2. myNetwork = xPipeLine2.getNetworkVessels(this.vessel);

            foreach (var v in myNetwork)
            {
                print(v.RevealName());
                pipeCount = 0;
                if (v.loaded)
                    foreach (var pm in v.FindPartModulesImplementing<xPipeLine2>())
                        pipeCount+=4;
               
                    
            }
            currentPipe = 0;
            //{
            //    dbShipResources resources = new dbShipResources();
            //    foreach (var part in v.protoVessel.protoPartSnapshots)
            //    {
            //        foreach (var res in part.resources)
            //        {
                        
            //                double available = double.Parse(res.resourceValues.GetValue("amount"));
            //                double capacity = double.Parse(res.resourceValues.GetValue("maxAmount"));
            //                aggregateDictionary(res.resourceName, available, ref resources.amounts);
            //                aggregateDictionary(res.resourceName, capacity, ref resources.capcity);
                        
            //        }
            //    }

            //}

        }
        public static int pipeCount = 0;
        public static int currentPipe = 0;
        public static UrlDir.UrlConfig[] configs;
        public static ConfigNode findPartConfig(string name)
        {
            if(configs == null)
                configs =   GameDatabase.Instance.GetConfigs("PART");
            foreach (var config in configs)
                if (config.name == name)
                    return config.config;
            return null;

        }

        public class powerCurveFunction
        {
            List<Vector2d> fnodes;
            
            public void setFunction(string [] f)
            {
                fnodes = new List<Vector2d>();
                foreach(string n in f)
                {
                    string [] ns = n.Split(' ');
                    Vector2d node = new Vector2d(double.Parse(ns[0]), double.Parse(ns[1]));
                    fnodes.Add(node);
                }

            }
           
            public double getMultiplier(double alt)
            {
                if (fnodes == null)
                    return 0;
                for(int i = 0; i < fnodes.Count -1; i++)
                {
                    if (fnodes[i].x > alt && fnodes[i + 1].x <= alt)
                        return lerp(fnodes[i].x, fnodes[i + 1].x, fnodes[i].y, fnodes[i + 1].y, alt);
                }
                return 0;
            }

        }
        public static string getNonPersistantValue(ConfigNode node, string value)
        {
            string ret = node.GetValue(value);
            if (ret == null) return "";
            return ret;
        }

        static double sunScale(Vessel v, out double sunAltitude)
        {
            //find the star the planet is orbiting
            CelestialBody mainBody = v.mainBody;
            Vector3d toSun = Sun.Instance.sunDirection;
            Vector3d shipBodyNormal =   v.mainBody.position- v.GetWorldPos3D();
            shipBodyNormal.Normalize();
            //fun math time!
           // print(toSun);
           // print(shipBodyNormal);
            double c = Vector3d.Distance(toSun, shipBodyNormal);
            //         S
            //
            //    V
            //         p
            //
            //THe length of Vector VS is sqrt of 2, or 4.  The sunlight the ship is receiving is thus this:
            double sunlightMultiplier = lerp(0, 4, 1, 0, c * c); //manhattan distance yo!
            if (sunlightMultiplier < 0)
                sunlightMultiplier = 0;
            sunAltitude = v.altitude;
            while( mainBody != Sun.Instance.sun)
            {
                sunAltitude += mainBody.RevealAltitude();
                mainBody = mainBody.referenceBody;
            }
           // print(sunAltitude + " " + sunlightMultiplier);
            return sunlightMultiplier;
        }

        public static void updateNetworkResources(double dt)
        {
           // print("updateNwres " + dt);
            //if (dt < 0)
                return;
            dt *= 4;
            if(currentPipe == 0)
            {
               // print("update vessels!");
                foreach(Vessel v in myNetwork)
                {
                    if (v.loaded)
                        continue; //ignore loaded vessel
                    //only generators will be simulated!  Must add generators from all known mods!
                    //everyone is different too!
                    //supported generators will be processed in real time, unsupported will not
                    
                    foreach(var part in v.protoVessel.protoPartSnapshots)
                    {



                        ConfigNode node = findPartConfig(part.partName);
                        if (node == null)
                            continue;
                        // print(node);
                        var modules = node.GetNodes("MODULE");
                      
                        int moduleID = 0;

                        //solar power efficiency boost:
                        double solarOut = 0;
                        double panelAltitude = v.mainBody.RevealAltitude();
                        double sunMultiplier = sunScale(v, out panelAltitude);
                        bool hasSolar = false;
                        double altitudeMultiplier = 0;
                        double chargeRate = 0;
                        string solarResourceName = "ElectricCharge";

                        foreach(var module in part.modules)
                        {
                           // print("module id " + moduleID);
                            if (moduleID > modules.Length) //found unexpected module!... there won't be any new generators here
                                continue; 
                            
                           switch(module.moduleName)
                           {
                               case "ModuleGenerator":
                                   
                                   {
                                       try { 
                                      // print("found ModuleGenerator" + moduleID.ToString());
                                       ConfigNode nonPersistantValues = modules[moduleID];
                                       var outputResource = nonPersistantValues.GetNode("OUTPUT_RESOURCE");
                                       var inputResource = nonPersistantValues.GetNode("INPUT_RESOURCE");
                                       string resource = outputResource.GetValue("name");
                                       double rate = double.Parse(outputResource.GetValue("rate"));
                                      
                                       string iResource = "";
                                       double iRate = 0;
                                           if(inputResource != null)
                                           {
                                               iResource = inputResource.GetValue("name");
                                               iRate = double.Parse(inputResource.GetValue("rate"));
                                               double input = networkResourceRequest(v, iResource, iRate * dt);
                                               double iScale = input / (iRate * dt);

                                               double output = networkResourceRequest(v, resource, -rate * iScale * dt);
                                               double oscale = output / (-rate * iScale * dt);
                                               double irefund = input * (1 - oscale);
                                               networkResourceRequest(v, iResource, irefund);

                                           }
                                           
                                           }
                                       catch
                                       {
                                           print("Failed to update ModuleGenerator module, check for updates");
                                       }
                                           
                                   }
                                   break;
                              // case "KolonyConverter":
                               case "REGO_ModuleResourceConverter":
                               case "REGO_ModuleResourceHarvester":
                                   {
                                       try
                                       {
                                           ConfigNode nonPersistantValues = modules[moduleID];
                                           string recipeInputs = nonPersistantValues.GetValue("RecipeInputs");
                                           string recipeOutputs = nonPersistantValues.GetValue("RecipeOutputs");
                                           double groundEfficiency = 1;
                                           if(module.moduleName == "REGO_ModuleResourceHarvester")
                                           {
                                               string resourcename = nonPersistantValues.GetValue("ResourceName");
                                               recipeOutputs = resourcename + ",1";
                                               var abRequest = new AbundanceRequest
                                               {
                                                   Altitude = v.altitude,
                                                   BodyId = FlightGlobals.currentMainBody.flightGlobalsIndex,
                                                   CheckForLock = false,
                                                   Latitude = v.latitude,
                                                   Longitude = v.longitude,
                                                   ResourceType = (HarvestTypes)0,
                                                   ResourceName = resourcename
                                               };
                                               var abundance = ResourceMap.Instance.GetAbundance(abRequest);
                                               groundEfficiency = abundance;
                                           }

                                           string RequiredResources = nonPersistantValues.GetValue("RequiredResources");
                                           if (recipeInputs == null) recipeInputs = "";
                                           if (recipeOutputs == null) recipeOutputs = "";
                                           if (RequiredResources == null) RequiredResources = "";
                                          // print(recipeInputs);
                                          // print(recipeOutputs);
                                          // print(RequiredResources);

                                           string isEnabled = module.moduleValues.GetValue("IsActivated");
                                           double efficiency = double.Parse(module.moduleValues.GetValue("EfficiencyBonus"));
                                          
                                           bool harvesterOK = true;
                                           
                                           if (isEnabled.ToLower() == "true" && harvesterOK)
                                           {
                                               var localdelta = processRegolithGenerator(v, dt * efficiency * groundEfficiency, recipeInputs, recipeOutputs, RequiredResources);
                                               foreach (string k in localdelta.Keys)
                                                   networkResourceRequest(v, k, -localdelta[k]);
                                           }
                                           module.moduleValues.SetValue("lastUpdateTime", Planetarium.GetUniversalTime().ToString());
                                           /*
                                            * Regolith generators use these 
                                            isEnabled = True
                                            EfficiencyBonus = 1
                                            IsActivated = False
                                            DirtyFlag = False
                                            lastUpdateTime = 49904539.6623243
                                            */
                                       }
                                       catch (Exception exc)
                                       {
                                           print("Failed to update regolith module, check for updates:" + exc.ToString());
                                       }
                                   }
                                   break;
                               case "NetworkBroker2":
                                   {
                                       //network broker tries to take network input from the network.
                                      // print("found NetworkBroker2");
                                       try {
                                           ConfigNode nonPersistantValues = modules[moduleID];

                                           string NetworkInput = nonPersistantValues.GetValue("NetworkInput");
                                           string isEnabled = module.moduleValues.GetValue("IsActivated");
                                           bool IsActivated = bool.Parse(isEnabled);

                                           string[] NWIn = NetworkInput.Split(',');
                                           double inputRequest = double.Parse(NWIn[1]) * dt;
                                          string NetworkResource = NWIn[0];
                                          double NetworkDraw = 0;
                                           if (/*connected*/true && IsActivated && inputRequest > 0)
                                           {

                                               double nwdraw = xPipeLine2.fullNetworkRequest(v, NetworkResource, inputRequest);
                                               NetworkDraw = nwdraw / dt;
                                               double stored = xPipeLine2.networkResourceRequest(v,NWIn[0], -nwdraw);
                                               double excess = nwdraw + stored;
                                              // print("NetworkBroker2 " + nwdraw.ToString() + " store " + stored.ToString() + " excess " + excess.ToString());
                                               xPipeLine2.fullNetworkRequest(v, NWIn[0], -excess);
                                           }
                                       }
                                       catch
                                       {
                                           print("Failed to update NetworkBroker2 module, check for updates");
                                       }
                                   }
                                   break;
                               case "xPipeLine2": //update current time on pipes so regolith doesn't simulate them!
                                   {
                                       try {
                                           ConfigNode nonPersistantValues = modules[moduleID];
                                       module.moduleValues.SetValue("lastUpdateTime", Planetarium.GetUniversalTime().ToString());
                                       }
                                       catch
                                       {
                                           print("Failed to update xPipeLine2 module, check for updates");
                                       }
                                   }
                                   break;
                               case "ModuleDeployableSolarPanel":
                                   {
                                       try
                                       {
                                           ConfigNode nonPersistantValues = modules[moduleID];
                                         
                                           bool sunTracking = true;
                                           if (!bool.TryParse(getNonPersistantValue(nonPersistantValues, "sunTracking"), out sunTracking))
                                               sunTracking = true; //squad doesn't mark suntracking on sun tracking panels!
                                           solarResourceName = getNonPersistantValue(nonPersistantValues, "resourceName");
                                           chargeRate += double.Parse(getNonPersistantValue(nonPersistantValues, "chargeRate"));
                                           ConfigNode cpowerCurve = nonPersistantValues.GetNode("powerCurve");
                                           string[] keys = cpowerCurve.GetValues("key");

                                           powerCurveFunction func = new powerCurveFunction();
                                           func.setFunction(keys);
                                          
                                           double multiplier = func.getMultiplier(panelAltitude);
                                           if(!hasSolar)
                                           {
                                               altitudeMultiplier = multiplier;
                                           }
                                           multiplier *= sunMultiplier; //simple half duty cycle for unfocused solar panels
                                           solarOut++;
                                           /*MODULE
                                            {
                                                name = ModuleDeployableSolarPanel

                                                sunTracking = false

                                                raycastTransformName = suncatcher
                                                pivotName = suncatcher

                                                isBreakable = false

                                                resourceName = ElectricCharge

                                                chargeRate = 0.75

                                                powerCurve
                                                {
                                                    key = 206000000000 0 0 0
                                                    key = 13599840256 1 0 0
                                                    key = 68773560320 0.5 0 0
                                                    key = 0 10 0 0
                                                }
                                            }*/
                                       }
                                       catch (Exception exc)
                                       {
                                           print("Failed to update solar module, check for updates" + exc.ToString());
                                       }
                                   }
                                   break;
                               default:
                                   break;
                           }

                           moduleID++;        
                        }
                        if(hasSolar)
                        {
                            networkResourceRequest(v, solarResourceName, -chargeRate * dt * altitudeMultiplier * sunMultiplier);
                        }
                    }
                }

               
            }
            currentPipe++;
            if (currentPipe > pipeCount)
                currentPipe = 0;
            

        }
        public static double lerp(double a1, double a2, double b1, double b2, double ai)
        {
            return b1 + (b2 - b1) / (a2 - a1) * (ai - a1);
        }
        public static Dictionary<string, double> processRegolithGenerator(Vessel v,double dt, string recipeInputs, string recipeOutputs,string RequiredResources)
        {
           // print("z");
            string[] inputs = recipeInputs.Split(',');
            string[] outputs = recipeOutputs.Split(',');
            string[] required = RequiredResources.Split(',');
            Dictionary<string, double> xinputs = new Dictionary<string, double>();
            Dictionary<string, double> xoutputs = new Dictionary<string, double>();
            Dictionary<string, bool> outputOverflows = new Dictionary<string, bool>();
            Dictionary<string, double> xrequired = new Dictionary<string, double>();
           // print("a");
            
#region parseRego
            string tempRes = "";
            for (int i = 0; i < inputs.Length; i++)
            {
                if (i % 2 == 0)
                    tempRes = inputs[i];
                if (i % 2 == 1)
                    xinputs[tempRes] = double.Parse(inputs[i]) * dt;
            }
            for (int i = 0; i < required.Length; i++)
            {
                if (i % 2 == 0)
                    tempRes = required[i];
                if (i % 2 == 1)
                    xrequired[tempRes] = double.Parse(required[i]);
            }
            for (int i = 0; i < outputs.Length; i++)
            {
                if (i % 3 == 0)
                    tempRes = outputs[i];
                if (i % 3 == 1)
                    xoutputs[tempRes] = double.Parse(outputs[i]) * dt;
                if (i % 3 == 2)
                    outputOverflows[tempRes] = bool.Parse(outputs[i]);
            }
#endregion
          //  print("ab");
            Dictionary<string, Vector2d> resourceStats = new Dictionary<string, Vector2d>();
            foreach(string x in xinputs.Keys)
                if(!resourceStats.ContainsKey(x))
                {
                    double amt, cap;
                    networkResourceStats(v, x, out amt, out cap);
                    resourceStats[x] = new Vector2d(amt, cap);
                }
            foreach (string x in xoutputs.Keys)
                if (!resourceStats.ContainsKey(x))
                {
                    double amt, cap;
                    networkResourceStats(v, x, out amt, out cap);
                    resourceStats[x] = new Vector2d(amt, cap);
                }
            foreach (string x in xrequired.Keys)
                if (!resourceStats.ContainsKey(x))
                {
                    double amt, cap;
                    networkResourceStats(v, x, out amt, out cap);
                    resourceStats[x] = new Vector2d(amt, cap);
                }
           // print("ac");
            double minInputScale = 1;
            foreach(string x in xinputs.Keys)
            {
                double inputscale = capResource(xinputs[x], resourceStats[x].x, resourceStats[x].y) / xinputs[x];
                if (inputscale < minInputScale)
                    minInputScale = inputscale;
            }
           // print("ad" + minInputScale.ToString());
            foreach (string x in xrequired.Keys)
                if (resourceStats[x].x < xrequired[x])
                    minInputScale = 0;
            double minOutputScale = 1;
           foreach(string x in xoutputs.Keys)
           {
               double outputScale = capResource(-xoutputs[x] * minInputScale, resourceStats[x].x, resourceStats[x].y) / (-xoutputs[x] * minInputScale);
              // print(x + " " + outputScale.ToString());
               if (outputScale < minOutputScale && !outputOverflows[x])
                   minOutputScale = outputScale;
           }
          // print("ae");
           Dictionary<string, double> localresourceDeltas = new Dictionary<string, double>();
            foreach(string x in xinputs.Keys)
            {
                if (localresourceDeltas.ContainsKey(x))
                    localresourceDeltas[x] -= xinputs[x] * minInputScale * minOutputScale;
                else
                    localresourceDeltas[x] = -xinputs[x] * minInputScale * minOutputScale;

            }
           // print("af");
            foreach (string x in xoutputs.Keys)
            {
                if (localresourceDeltas.ContainsKey(x))
                    localresourceDeltas[x] += xoutputs[x] * minInputScale * minOutputScale;
                else
                    localresourceDeltas[x] = xoutputs[x] * minInputScale * minOutputScale;

            }
           // xPipeLine.debugDictionary("regodelta", localresourceDeltas);
            return localresourceDeltas;


        }
        public static double capResource(double req, double amt, double cap)
        {
            double temp = Math.Abs(req);
            if (amt < 0)
                amt = 0;
            if (amt > cap)
                amt = cap;
            //calculate what we can request based on the direction req is going
            if (req < 0)
            {
                //req is increasing.
                return -Math.Min(cap - amt, temp);
            }
            if (req > 0)
                return Math.Min(amt, temp);
            return 0;

        }
        public override void OnLoad(ConfigNode node)
        {
           
            //build the local database
            base.OnLoad(node);
        }
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
        }
        Vector3d networkLocation = Vector3d.zero;
        public override void OnActive()
        {

            if (HighLogic.LoadedSceneIsFlight)
            {
                print("add pipe");
              
                loadNetwork();
                networkLocation = new Vector3d(vessel.latitude, vessel.longitude, vessel.altitude);
            }
            
            base.OnActive();
            
        }
        public override void OnInactive()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                print("remove pipe");
                loadNetwork();
            }
            base.OnInactive();
        }
         public override void OnUpdate()
         {
             base.OnUpdate();
         }
         void aggregateDictionary(string key, double value, ref Dictionary<string, double> dic)
         {
             if (dic.ContainsKey(key))
                 dic[key] += value;
             else
                 dic[key] = value;
         }
        public static int countVesselsWithResource(string resource, Vessel requestor)
         {
             int count = 0;
             foreach (Vessel v in myNetwork)
             {
                 if (requestor != null && requestor == v)
                     continue;
                 foreach (var part in v.protoVessel.protoPartSnapshots)
                 {
                     bool hasResource = false;
                     foreach (var res in part.resources)
                     {

                         if (res.resourceName == resource)
                         {
                             hasResource = true;
                             break;
                         }
                         
                         //aggregateDictionary(res.resourceName, available, ref resources.amounts);
                         //aggregateDictionary(res.resourceName, capacity, ref resources.capcity);

                     }
                     if (hasResource)
                     {
                         count++;
                         break;
                     }
                 }
             }
             return count;
         }
        public static double fullNetworkRequest(Vessel requestor, string resource, double amount)
         {
             int count = countVesselsWithResource(resource, requestor);
             double reqPerVessel = amount / count;
            // print("count = " + count.ToString());
            // print("amount = " + amount.ToString());
             //print("requestPerShip = " + reqPerVessel.ToString());
            
             double requested = 0;
             foreach (Vessel v in myNetwork)
                 if (v == requestor)
                     continue;
                 else
                     requested += networkResourceRequest(v, resource, reqPerVessel);
             return requested;
         }
        public static void networkResourceStats(Vessel v, string resource, out double available, out double capacity)
        {
            if (v.loaded)
            {
                var resourceDef = v.GetActiveResources().Find(res => res.info.name == resource);
                if (resourceDef != null)
                {
                    available = resourceDef.amount;
                    capacity = resourceDef.maxAmount;
                    return ; //use this for LAN ships
                }
                else
                {
                    available = 0;
                    capacity = 0;
                    return;
                }
            }
            else
            {

                 available = 0;
                 capacity = 0;
                double count = 0;


                foreach (var part in v.protoVessel.protoPartSnapshots)
                {
                    foreach (var res in part.resources)
                    {
                        if (res.resourceName == resource)
                        {
                            //print(res.resourceName + " " + res.resourceValues.GetValue("amount") + " " + res.resourceValues.GetValue("maxAmount"));
                            available += double.Parse(res.resourceValues.GetValue("amount"));
                            capacity += double.Parse(res.resourceValues.GetValue("maxAmount"));
                            //aggregateDictionary(res.resourceName, available, ref resources.amounts);
                            //aggregateDictionary(res.resourceName, capacity, ref resources.capcity);
                            count++;
                        }
                    }
                }
              

            }





        }
         public static double networkResourceRequest(Vessel v, string resource, double amount)
         {
             if (amount == 0)
                 return 0; //performance boost!
             if(v.loaded)
             {
                 return v.parts[0].RequestResource(resource, amount); //use this for LAN ships
             }
             else
             {
                
                 double available = 0;
                 double capacity = 0;
                 double count = 0;
                
                    
                     foreach (var part in v.protoVessel.protoPartSnapshots)
                     {
                         foreach (var res in part.resources)
                         {
                             if (res.resourceName == resource)
                             {
                                // print(res.resourceName + " " + res.resourceValues.GetValue("amount") + " " + res.resourceValues.GetValue("maxAmount"));
                                 available += double.Parse(res.resourceValues.GetValue("amount"));
                                 capacity += double.Parse(res.resourceValues.GetValue("maxAmount"));
                                 //aggregateDictionary(res.resourceName, available, ref resources.amounts);
                                 //aggregateDictionary(res.resourceName, capacity, ref resources.capcity);
                                 count++;
                             }
                         }
                     }
                     double canrequest = 0;
                     canrequest = capResource(amount, available, capacity);
                    // print("canreq = " +canrequest.ToString());
                     double adjust = canrequest / count;
                    // print("adjust = " + adjust.ToString());
                     foreach (var part in v.protoVessel.protoPartSnapshots)
                     {
                         foreach (var res in part.resources)
                         {
                             if (res.resourceName == resource)
                             {
                                 available = double.Parse(res.resourceValues.GetValue("amount"));
                                
                                     available -= adjust;
                                
                                 
                                 res.resourceValues.SetValue("amount", available.ToString());
                                 
                             }
                         }
                     }
                    
                     return canrequest;

                 }   
                 
                 
             
            
             
         }

        public static bool vesselCanJoinNetwork(Vessel v)
         {
             if (v.loaded)
             {
                 var modules = v.FindPartModulesImplementing<xPipeLine2>();
                 foreach (var m in modules)
                     if (m.Connected)
                         return true;
                 return false;
             }


             foreach (var part in v.protoVessel.protoPartSnapshots)
             {
                 foreach (var module in part.modules)
                 {
                     if (module.moduleName == "xPipeLine2")
                     {
                         print("vessel pipeline found");
                         if (module.moduleValues.GetValue("Connected").ToLower() == "true")
                             return true;
                     }
                 }

             }
             return false;
         }

         public static Vessel[] getNetworkVessels(Vessel v)
         {
             print("Getting vessels...");
             List<Vessel> network = new List<Vessel>();
             if(vesselCanJoinNetwork(v))
             network.Add(v);
             foreach(Vessel xv in FlightGlobals.Vessels)
             {
                 print("vessel wants to join NW:" + xv.name);
                 if (xv.mainBody.name != FlightGlobals.currentMainBody.name)
                     continue; //ignore ships not in the same celestial system
                 print("body pass" + xv.name);
                 if (network.Contains(xv))
                     continue; //vessel already in network
                 if(network.Count == 0 && xv.loaded)
                 {
                     if (vesselCanJoinNetwork(xv))
                         network.Add(xv);
                     continue;
                 }
                 if (vesselCanJoinNetwork(xv))
                 {
                     print("vessel wants to join NW:"  + xv.name  );
                     foreach (Vessel yv in network)
                     {
                         print(" dist is " + Vector3d.Distance(xv.GetWorldPos3D(), yv.GetWorldPos3D()));
                         if (Vector3d.Distance(xv.GetWorldPos3D(), yv.GetWorldPos3D()) < 10000)
                         {

                             network.Add(xv);
                             break;
                         }
                     }
                 }
             }
             return network.ToArray();
         }

        public double distanceToClosestVessel()
        {
            double distance = 9999999999;
            if (myNetwork.Length < 1)
                return 0; //only 1 vessel on the network
            foreach(Vessel v in myNetwork)
            {
               
                if(Vector3d.Distance(vessel.GetWorldPos3D(), v.GetWorldPos3D()) < distance)
                    distance = Vector3d.Distance(vessel.GetWorldPos3D(), v.GetWorldPos3D());
            }
          
            return distance;
        }

         [KSPField(isPersistant=true)]
         public string ResourceDeltas;

         public Dictionary<string, double> resourceLastAmounts;
         public Dictionary<string, double> deltas;
        
       
       
        [KSPEvent(guiActive = true, guiName = "Connect Network", active = true)]
        public void StartConnection()
        {
            if(vessel.situation != Vessel.Situations.LANDED)
            {
                ScreenMessages.PostScreenMessage("Must be landed to start connecting.");
                return;
            }
            buildCost = calculateBuildCost();
            StartResourceConverter();
            Events["StartConnection"].active = false;
            Events["StopConnection"].active = true;
            Fields["buildCost"].guiActive = true;
        }
        [KSPEvent(guiActive = true, guiName = "Continue Connecting", active = false)]
        public void ContinueConnection()
        {
            Events["ContinueConnection"].active = false;
            Events["StopConnection"].active = true;
            Events["CancelConnection"].active = false;
            //buildCost = calculateBuildCost();
            StartResourceConverter();
        }
        [KSPEvent(guiActive = true, guiName = "Stop Connecting", active = false)]
        public void StopConnection()
        {
           // buildCost = calculateBuildCost();
            Events["StopConnection"].active = false;
            Events["ContinueConnection"].active = true;
            Events["CancelConnection"].active = true;
          
            StopResourceConverter();
        }
        [KSPEvent(guiActive = true, guiName = "Cancel Connecting", active = false)]  //these get saved!
        public void CancelConnection()
        {
            // buildCost = calculateBuildCost();
            StopResourceConverter();
            Events["ContinueConnection"].active = false;
            ConfirmDisconnectNetwork();
            connectionInProgress = false;
        }

        [KSPEvent(guiActive = true, guiName = "Disconnect Network", active = false)]
        public void DisconnectNetwork()
        {
            disconnectConfirmTimer = 5;
            Events["ConfirmDisconnectNetwork"].active = true;
            Events["DisconnectNetwork"].active = false;
          
        }
        double disconnectConfirmTimer = 0;
        [KSPEvent(guiActive = true, guiName = "Confirm Disconnect Network", active = false)]
        public void ConfirmDisconnectNetwork()
        {
            Connected = false;
            Events["ConfirmDisconnectNetwork"].active = false;
            ScreenMessages.PostScreenMessage("Network Disconnected!", 5, ScreenMessageStyle.UPPER_CENTER);
            Events["StartConnection"].active = true;
            loadNetwork();
        }
         public override void OnFixedUpdate()
         {
             double dt = GetDeltaTimex();

             if (vessel.loaded)
                 vessel.checkLanded();
             //print(vessel.RevealSituationString());
             if(vessel.situation == Vessel.Situations.LANDED && !Connected && Events["StartConnection"].active)
             {
                 buildCost = calculateBuildCost();
                 Fields["buildCost"].guiActive = true;
             }
            
             
                
             Events["StartResourceConverter"].active = false;
             Events["StopResourceConverter"].active = false;
             if (Events["ConfirmDisconnectNetwork"].active && disconnectConfirmTimer <= 0)
             {
                 if(Connected)
                    Events["DisconnectNetwork"].active = true;
                 Events["ConfirmDisconnectNetwork"].active = false;
                 
             }
             else if (!Events["ConfirmDisconnectNetwork"].active)
             {
                 Events["DisconnectNetwork"].active = Connected;
             }
             if(disconnectConfirmTimer >0)
                 disconnectConfirmTimer -= dt;
             if(Connected)
             {
                 //calculate the pipe break timer
                 //print("Break time = " + pipeBreakTimer);
                 //print(vessel.IsClearToSave());
                 //vessel breaks network if it moves more than 4 meters from where it joined the network
                 double dist = Vector3d.Distance(vessel.mainBody.GetWorldSurfacePosition(vessel.latitude,vessel.longitude,vessel.altitude), vessel.mainBody.GetWorldSurfacePosition(networkLocation.x, networkLocation.y, networkLocation.z));
                // print("vessel moved " + dist);
                 if (dist > 4)
                     ConfirmDisconnectNetwork();
                 //if (vessel.situation != Vessel.Situations.LANDED || vessel.IsClearToSave() == ClearToSaveStatus.NOT_WHILE_MOVING_OVER_SURFACE)
                 //{
                 //    pipeBreakTimer += dt;
                 //    if (pipeBreakTimer > breakDuration)
                 //        ConfirmDisconnectNetwork();
                 //}
                 //else
                 //    pipeBreakTimer = 0;

                
             }
             var brokers = part.FindModulesImplementing<NetworkBroker2>();
             foreach (var b in brokers)
             {
                 b.connected = Connected;
             }

             updateNetworkResources(dt);
            // if(!Connected)
                base.OnFixedUpdate();
            
             if(status != "Missing inputs" && !Connected && IsActivated)
             {
                 Fields["buildCost"].guiActive = true;
                 buildCost -= (float)(costPerMeter * dt * EfficiencyBonus);
                 if (buildCost <= 0)
                 {
                     Connected = true;
                     loadNetwork();
                     ScreenMessage sm = new ScreenMessage("Network Connected!", 5, ScreenMessageStyle.UPPER_CENTER);
                     ScreenMessages.PostScreenMessage(sm);
                     StopResourceConverter();
                     Events["StopConnection"].active = false;
                     Events["ContinueConnection"].active = false;
                     Events["CancelConnection"].active = false;
                     Events["StartConnection"].active = false;
                     
                     Fields["buildCost"].guiActive = false;

                     networkLocation = new Vector3d(vessel.latitude, vessel.longitude, vessel.altitude);
                 }
             }

             

         }
         #region regolith
       

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
         public void reloadRecipe()
         {
             //RecipeOutputs = pipelineShip.DictionaryFromString(RecipeOutputs);
         }
         private ConversionRecipe LoadRecipe()
         {
             var r = new ConversionRecipe();
             // return r;
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
         protected double GetDeltaTimex()
         {
             try
             {
                 if (Time.timeSinceLevelLoad < 1.0f || !FlightGlobals.ready)
                 {
                     return -1;
                 }

                 if (Math.Abs(lastUpdateTime) < ResourceUtilities.FLOAT_TOLERANCE)
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

     }
    public class NetworkBroker2 : BaseConverter
    {

        //When this generator is active the broker adds input bandwidth to the network
        //c



        //broker supply is how much of the resource the ship is generating at this time
        public Dictionary<string, double> supply;
        //broker demand is how much this ship is pulling from the network
        public Dictionary<string, double> demand;

        public string NetworkResource;
        public double NetworkDraw;

        public bool connected;
        protected double GetDeltaTimex()
        {
            try
            {
                if (Time.timeSinceLevelLoad < 1.0f || !FlightGlobals.ready)
                {
                    return -1;
                }

                if (Math.Abs(lastUpdateTime) < ResourceUtilities.FLOAT_TOLERANCE)
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
            double dt = GetDeltaTimex();

            //the broker supplies the recipeinputs per second to the network
            //request the input resource
            string[] NWIn = NetworkInput.Split(',');
            double inputRequest = double.Parse(NWIn[1]) * dt;
            NetworkResource = NWIn[0];
            NetworkDraw = 0;
            if (connected && base.IsActivated && inputRequest > 0)
            {

                double nwdraw = xPipeLine2.fullNetworkRequest(this.vessel, NetworkResource, inputRequest);
                NetworkDraw = nwdraw / dt;
                double stored = part.RequestResource(NWIn[0], -nwdraw);
                double excess = nwdraw + stored;
              //  print("converter2 " + nwdraw.ToString() + " store " + stored.ToString() + " excess " + excess.ToString());
                xPipeLine2.fullNetworkRequest(this.vessel, NWIn[0], -excess);
            }
            else if (!connected)
                status = "not connected";


            base.OnFixedUpdate();
        }

        #region regolith
        [KSPField]
        public string NetworkInput = "";

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
        public void reloadRecipe()
        {
            _recipe = LoadRecipe();
            //RecipeOutputs = pipelineShip.DictionaryFromString(RecipeOutputs);
        }
        private ConversionRecipe LoadRecipe()
        {
            var r = new ConversionRecipe();
            return r;
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
    }
    #endregion
    //create the network scenario, then load it
    public class xPipeLine : BaseConverter
    {

        public Dictionary<string, double> resourceLastTime;
        public Dictionary<string, double> deltas;

        public Dictionary<string, double> amounts;
        public Dictionary<string, double> maximums;

        public Dictionary<string, double> networkDelta;

        public Dictionary<string, double> totalSupply;
        public Dictionary<string, double> totalDemand;
        public Dictionary<string, double> totalCapacity;  //if supply is less than demand, this is how long the resource will last.

        public Dictionary<string, double> networkConsumed;
        bool firstNetworkSubmit = true;

       
        public bool master = false;
        public bool localMaster = false;
        public void setConsumed(string resource, double amount)
        {
            if (networkConsumed == null)
                networkConsumed = new Dictionary<string, double>();
            if (!networkConsumed.ContainsKey(resource))
                networkConsumed[resource] = amount;
            else
                networkConsumed[resource] += amount;

        }
        public override void OnInactive()
        {
            print("ship unloaded");
            PipeLineNetworkManager.instance.setShipLoaded(this, false);
            base.OnInactive();
        }
        public void updateLanState()
        {
            //build the lan
            //lan inside the part
            bool canBeMaster = true;
            var pipes = vessel.FindPartModulesImplementing<xPipeLine>();
            foreach (var pipe in pipes)
                if (pipe.master)
                    canBeMaster = false;
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (Vector3d.Distance(v.GetWorldPos3D(), vessel.GetWorldPos3D()) < vessel.distanceLandedUnpackThreshold)
                {
                    var vpipes = v.FindPartModulesImplementing<xPipeLine>();
                    foreach (var pipe in vpipes)
                        if (pipe.master)
                            canBeMaster = false;
                }
            }
            if (canBeMaster)
                master = true;
            canBeMaster = true;
            pipes = vessel.FindPartModulesImplementing<xPipeLine>();
            foreach (var pipe in pipes)
                if (pipe.localMaster)
                    canBeMaster = false;
            localMaster = canBeMaster;
        }
        public override void OnStart(PartModule.StartState state)
        {
          if(state == StartState.Editor)
          { base.OnStart(state);
            return;

          }
          networkConsumed = new Dictionary<string, double>();
         
         //  part.mass = intendedMass - part.GetResourceMass();
            part.OnJustAboutToBeDestroyed = new Callback(deregisterShip);
            base.OnStart(state);
        }
        public override void OnActive()
        {
            print("ship loaded!");
            PipeLineNetworkManager.instance.setShipLoaded(this, true);
            PipeLineNetworkManager.instance.resetShipResources(this);
            setResources(0);
            setCurrentNetwork(0);
            base.OnActive();
        }
        protected double GetDeltaTimex()
        {
            try
            {
                if (Time.timeSinceLevelLoad < 1.0f || !FlightGlobals.ready)
                {
                    return -1;
                }

                if (Math.Abs(lastUpdateTime) < ResourceUtilities.FLOAT_TOLERANCE)
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
        public static void debugDictionary(string name, Dictionary<string, double> dic)
        {
            MonoBehaviour.print(name);
            foreach (string key in dic.Keys)
                print(key + "=" + dic[key].ToString());
        }
        public void setResources(double dt)
        {

            var brokerList = vessel.FindPartModulesImplementing<NetworkBroker>();
            //brokers are generators supplying a resource to the network
            //for each broker active, increase this ship's supply in units per second.

            totalSupply = new Dictionary<string, double>(); //units per second!  supply is calculated as how much the ship is producing per second...

            totalDemand = new Dictionary<string, double>();
            totalCapacity = new Dictionary<string, double>();
            networkDelta = new Dictionary<string, double>();
            {
                foreach (var brooker in brokerList)
                {
                    if (brooker.NetworkResource != string.Empty)
                    {
                        brooker.connected = connected;
                        networkDelta[brooker.NetworkResource] = brooker.NetworkDraw;
                    }
                }
                if (resourceLastTime == null)
                    resourceLastTime = new Dictionary<string, double>();
                Dictionary<string, double> activeResourceList = new Dictionary<string, double>();
               
                debugDictionary("networkConsume", networkConsumed);
                Dictionary<string, double> amounts = new Dictionary<string, double>();
                Dictionary<string, double> maximums = new Dictionary<string, double>();
                foreach (Part p in vessel.parts)
                    foreach (var res in p.Resources.list)
                    {
                        if (amounts.ContainsKey(res.info.name))
                            amounts[res.info.name] += res.amount;
                        else
                            amounts[res.info.name] = res.amount;
                        if (maximums.ContainsKey(res.info.name))
                            maximums[res.info.name] += res.maxAmount;
                        else
                            maximums[res.info.name] = res.maxAmount;
                    }

               foreach(string res in amounts.Keys)
                {
                    totalCapacity[res] = maximums[res];
                    totalSupply[res] = amounts[res];
                    if (networkConsumed.ContainsKey(res))
                    {
                        totalSupply[res] -= networkConsumed[res];
                        
                        networkConsumed[res] = 0;
                        
                    }
                    if (networkDelta.ContainsKey(res))
                        totalSupply[res] += networkDelta[res] * dt;

                    if (resourceLastTime.ContainsKey(res))
                        totalDemand[res] = (resourceLastTime[res] - totalSupply[res])/dt;
                    else
                        totalDemand[res] = 0;
                 
                  
                    resourceLastTime[res] = totalSupply[res];

                }

                //  debugDictionary("totalSupply", totalSupply);
                // debugDictionary("totalDemand", totalDemand);
                //  debugDictionary("totalCapacity", totalCapacity);
                // debugDictionary("resourceLastTime", resourceLastTime);
               // debugDictionary("networkdraw", networkDelta);
                //if (resourceLastTime == null)
                //{
                //    resourceLastTime = new Dictionary<string, double>();

                //}
                //else
                //{
                //    amounts = new Dictionary<string, double>();
                //    maximums = new Dictionary<string, double>();
                //    deltas = new Dictionary<string, double>();
                //    foreach (var part in vessel.parts)
                //    {
                //        if (part.FindModuleImplementing<xPipeLine>() != null)
                //            continue;
                //        foreach (var res in part.Resources.list)
                //        {


                //            if (!amounts.ContainsKey(res.info.name))
                //                amounts.Add(res.info.name, res.amount);
                //            else
                //                amounts[res.info.name] += res.amount;

                //            if (!maximums.ContainsKey(res.info.name))
                //                maximums.Add(res.info.name, res.maxAmount);
                //            else
                //                maximums[res.info.name] += res.maxAmount;


                //        }

                //    }
                //    foreach (string key in amounts.Keys)
                //    {
                //        if (!resourceLastTime.ContainsKey(key))
                //            resourceLastTime.Add(key, amounts[key]);


                //        if (!deltas.ContainsKey(key))
                //            deltas.Add(key, (resourceLastTime[key] - amounts[key]) / GetDeltaTimex());
                //        else
                //            deltas[key] = (resourceLastTime[key] - amounts[key])/GetDeltaTimex();

                //        resourceLastTime[key] = amounts[key];
                //    }

                //}




            }
        }
        
        public override void OnFixedUpdate()
        {
            updateLanState();
            double dt = GetDeltaTimex();
            if (master)
                PipeLineNetworkManager.instance.updateNetworkResources(dt);  //only master may trigger this
          
            setResources(dt);
            PipeLineNetworkManager.instance.unfocusedProducitivty(this);
               setCurrentNetwork(dt);
               base.StartResourceConverter();
              
           //need to restrict resource flow to this part only!
             
            //foreach(string s in networkDelta.Keys)
            //{
            //    part.RequestResource(s, -networkDelta[s] * dt, ResourceFlowMode.NO_FLOW);
            //}
            base.OnFixedUpdate();
            
           // part.mass = intendedMass - part.GetResourceMass();
        }

        [KSPField]
        public float intendedMass;

        [KSPField(isPersistant = true)]
        bool connected = false;

        [KSPField(isPersistant = true, guiActive=false, guiName = "Build Status")]
        float buildState = 0;


       void setCurrentNetwork(double dt)
        {

            var manager = PipeLineNetworkManager.instance;
           // print("Manager is null? " + (manager == null).ToString());
            if (connected)
            {

                PipeLineNetworkManager.instance.addShipToBodyNetwork(vessel, this, firstNetworkSubmit, dt);
                firstNetworkSubmit = false;
               // Events["connectNetwork"].active = false;
               // Events["disconnectNetwork"].active = true;
            }
            else
            {
               // Events["connectNetwork"].active = true;
               // Events["disconnectNetwork"].active = false;
            }

        }

        
        void deregisterShip()
        {
            PipeLineNetworkManager manager = PipeLineNetworkManager.instance;
            connected = false;
            manager.removeShipFromBodyNetwork(vessel, this);
           
        }


        [KSPEvent(guiName = "Connect Network", active = true, guiActive = true)]
        public void connectNetwork()
        {
            //register this ship on the network
            //todo: add construction time and cost

            connected = true;
        }
         [KSPEvent(guiName = "deConnect Network", active = true, guiActive = true)]
        public void disconnectNetwork()
        {
            //register this ship on the network
            //todo: add construction time and cost

            deregisterShip();
           
        }

        #region regolith
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
        public void reloadRecipe()
        {
            networkDelta = pipelineShip.DictionaryFromString(RecipeOutputs);
        }
        private ConversionRecipe LoadRecipe()
        {
            var r = new ConversionRecipe();
           // return r;
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




    }
    

    public class NetworkBroker: BaseConverter
    {

        //When this generator is active the broker adds input bandwidth to the network
        //c


     
        //broker supply is how much of the resource the ship is generating at this time
        public Dictionary<string, double> supply;
        //broker demand is how much this ship is pulling from the network
        public Dictionary<string, double> demand;

        public string NetworkResource;
        public double NetworkDraw;

        public bool connected;
        protected double GetDeltaTimex()
        {
            try
            {
                if (Time.timeSinceLevelLoad < 1.0f || !FlightGlobals.ready)
                {
                    return -1;
                }

                if (Math.Abs(lastUpdateTime) < ResourceUtilities.FLOAT_TOLERANCE)
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
            double dt = GetDeltaTimex();
           
            //the broker supplies the recipeinputs per second to the network
            //request the input resource
            string [] NWIn = NetworkInput.Split(',');
            double inputRequest = double.Parse(NWIn[1])* dt;
            NetworkResource = NWIn[0];
            NetworkDraw = 0;
            if (connected && base.IsActivated && inputRequest >0)
            {
                double nwdraw = PipeLineNetworkManager.instance.requestNetworkResource(this.vessel, NWIn[0], inputRequest );
                NetworkDraw = nwdraw / dt;
                double stored = part.RequestResource(NWIn[0], -nwdraw);
                double excess = nwdraw + stored;
               // print("converter " + nwdraw.ToString() + " store " + stored.ToString() + " excess " + excess.ToString());
                PipeLineNetworkManager.instance.requestNetworkResource(this.vessel, NWIn[0], -excess);
            }
            

                base.OnFixedUpdate();
        }

        #region regolith
        [KSPField]
        public string NetworkInput = "";

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
        public void reloadRecipe()
        {
            _recipe = LoadRecipe();
            //RecipeOutputs = pipelineShip.DictionaryFromString(RecipeOutputs);
        }
        private ConversionRecipe LoadRecipe()
        {
            var r = new ConversionRecipe();
            return r;
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
    }
}
