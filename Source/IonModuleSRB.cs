#define DEBUG_SRB
//#define DEBUG_UPDATES
//#define DEBUG_CALCULATIONS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using KSP;

namespace IoncrossKerbal_SRB
{
    public class IonModuleSRB : PartModule
    {
        public const string LIMITRESOURCE_AMOUNT = "limitingResource_Amount";
        public const string BURN_TIME_MAX = "burnTime_max";

        public ModuleEngines module_engine;

        public ConfigNode thrustCurveNode;
        public ThrustCurve thrustCurve;
        public IonGUIThrustCurve thrustCurve_guiController;

        public PartResource limitingResource;
        public Propellant limitingPropellant;
        public double limtingResource_initialAmount;

        //Flight GUI elements
        [KSPField(guiName = "Current Thrust Percent", isPersistant = false, guiActive = true, guiActiveEditor = false, guiFormat = "F1", guiUnits = "%")]
        public float curThrustPercent;


        //Editor GUI elements
        [KSPField(guiName = "Thrust Curve", isPersistant = false, guiActive = false, guiActiveEditor = true)]
        [UI_Toggle(disabledText = "", enabledText = "")]
        public bool editThrustCurve;

        [KSPField(guiName = "Current Thrust", isPersistant = false, guiActive = false, guiActiveEditor = true)]
        public float thrust;

        [KSPField(guiName = "Fuel Mass (t)", isPersistant = true, guiActive = false, guiActiveEditor = true)]
        public float fuelMass;
        public float fuelMass_last;

        [KSPField(guiName = "Burn Time (s)", isPersistant = true, guiActive = false, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 0.5f, maxValue = 300f, minValue = 0f)]
        public float burnTime; //[burnTime_min, burnTime_max]
        public float burnTime_last;
        public float burnTime_min;
        public float burnTime_max;

        //this will replace the module engine's thrust Limiter slider so the order of the modules doesn't affect the ordering of the interface
        [KSPField(guiName = "Thrust (%)", isPersistant = false, guiActive = false, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 0.5f, maxValue = 100f, minValue = 0f)]
        public float thrustPercent; //(0, 100]
        public float thrustPercent_last;
        public float thrustPercent_min;





        /************************************************************************\
         * IonModuleSRB class                                                   *
         * OnAwake function override                                            *
         *                                                                      *
        \************************************************************************/
        public override void OnAwake()
        {
            base.OnAwake();
#if DEBUG_SRB
            Debug.Log("IonModuleSRB.OnAwake() " + this.part.name);
#endif

            module_engine = (ModuleEngines)this.part.Modules["ModuleEngines"];

            thrustCurve = null;
            thrustCurveNode = null;
            thrustCurve_guiController = null;

            limitingResource = null;
        }
        /************************************************************************\
         * IonModuleSRB class                                                   *
         * OnLoad function                                                      *
         *                                                                      *
        \************************************************************************/
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
#if DEBUG_SRB
            Debug.Log("IonModuleSRB.OnLoad() " + this.part.name);
            Debug.Log("IonModuleSRB.OnLoad(): node\n" + node.ToString());
#endif
            burnTime_max = 300;
            limtingResource_initialAmount = 0;
            ParseHelper.ReadValue(node, BURN_TIME_MAX, ref burnTime_max);
            ParseHelper.ReadValue(node, LIMITRESOURCE_AMOUNT, ref limtingResource_initialAmount);

            foreach (ConfigNode subNode in node.nodes)
            {
                if (ThrustCurve.NODE_THRUST_CURVE == subNode.name)
                {
                    thrustCurveNode = subNode;
                }
            }
        }

        /************************************************************************\
         * IonModuleSRB class                                                   *
         * OnSave function                                                      *
         *                                                                      *
        \************************************************************************/
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
#if DEBUG_SRB
            Debug.Log("IonModuleSRB.OnSave() " + this.part.name);
#endif
            node.AddValue(BURN_TIME_MAX, burnTime_max);
            node.AddValue(LIMITRESOURCE_AMOUNT, limtingResource_initialAmount);

            ConfigNode subNode = new ConfigNode(ThrustCurve.NODE_THRUST_CURVE);

            if (null != thrustCurve)
            {
                thrustCurve.Save(subNode);
                node.AddNode(subNode);
            }
            else if (null != thrustCurveNode)
                node.AddNode(thrustCurveNode);

#if DEBUG_SRB
            Debug.Log("IonModuleSRB.OnSave(): node\n" + node.ToString());
#endif
        }


        /************************************************************************\
         * IonModuleSRB class                                                   *
         * OnStart function                                                     *
         *                                                                      *
        \************************************************************************/
        public override void OnStart(PartModule.StartState state)
        {
            base.OnStart(state);
#if DEBUG_SRB
            Debug.Log("IonModuleSRB.OnStart() " + this.part.name);
            Debug.Log("IonModuleSRB.OnStart(): state " + state.ToString());
#endif
            //setup ThrustCurve and guiController
            thrustCurve = new ThrustCurve();
            thrustCurve.Load(thrustCurveNode);
            thrustCurveNode = null;
            thrustCurve.minimumThrust = module_engine.minThrust / module_engine.maxThrust;

            thrustCurve_guiController = new IonGUIThrustCurve(this, thrustCurve);

            if (null != module_engine)
            {
                //If in editor or pre-launch
                if (0 != ((PartModule.StartState.Editor | PartModule.StartState.PreLaunch) & state) )
                {
                    //get limiting propellant and fuel mass
                    FindLimitingPropellant(out limitingPropellant, out limitingResource);
                    limtingResource_initialAmount = limitingResource.amount;
                    fuelMass = CalculateEffectiveFuelMass();

                    //set thrust values
                    thrust = module_engine.maxThrust * module_engine.thrustPercentage / 100f;
                    thrustPercent = module_engine.thrustPercentage;
                    thrustPercent_min = module_engine.minThrust / module_engine.maxThrust * 100f;

                    //calculate burnTime using on initial fuel load and thrust percentage
                    //calculate burnTime_min using full fuel load and max thrust
                    burnTime = CalculateBurnTime(module_engine.maxThrust * thrustPercent / 100f);
                    burnTime = RoundToHalf(burnTime);
                    burnTime_min = CalculateBurnTimeFullFuel(module_engine.maxThrust);

                    //update remaining data and curves
                    UpdateData();


                    //Set bounds for UI sliders
                    ((UI_FloatRange)(Fields["burnTime"].uiControlEditor)).minValue = burnTime_min;
                    ((UI_FloatRange)(Fields["burnTime"].uiControlEditor)).maxValue = burnTime_max;
                    ((UI_FloatRange)(Fields["thrustPercent"].uiControlEditor)).minValue = thrustPercent_min;

                    //engine's thrust limiter slider turned off (this module has its own)
                    module_engine.Fields["thrustPercentage"].guiActiveEditor = false;
#if DEBUG_SRB
                    Debug.Log("IonModuleSRB.OnStart(): burnTime " + burnTime + " | burnTime_min " + burnTime_min + " | engine.maxThrust " + module_engine.maxThrust + " | thrustPercent " + thrustPercent);
#endif
                }

            }
        }


        /************************************************************************\
         * IonModuleSRB class                                                   *
         * OnUpdate function                                                    *
         *                                                                      *
        \************************************************************************/
        public override void OnUpdate()
        {
            base.OnUpdate();

#if DEBUG_UPDATES
            Debug.Log("IonModuleSRB.OnUpdate() " + this.part.partName);
#endif
            //limtingResource_initialAmount is only calculate in OnStart and OnGUI (when in the editor)
            //it is therefore equal to the mass of fuel when the rocket is ignited
            curThrustPercent = thrustCurve.EvaluateFuel((float)(limitingResource.amount / limtingResource_initialAmount)) * 100f;
            module_engine.thrustPercentage = curThrustPercent;

#if DEBUG_UPDATES
            Debug.Log("IonModuleSRB.OnUpdate(): calculaating thrustPercentCurve for " + limitingResource.amount / limtingResource_initialAmount + " |  curThrustPercent " + curThrustPercent);
#endif
        }


        /************************************************************************\
         * IonModuleSRB class                                                   *
         * OnGUI function                                                       *
         *                                                                      *
         * OnGUI is used because OnUpate is not called in the editor.           *
        \************************************************************************/
        public void OnGUI()
        {
#if DEBUG_UPDATES
            Debug.Log("IonModuleSRB.OnGUI() " + this.part.partName);
#endif
            if (null != module_engine && HighLogic.LoadedSceneIsEditor)
            {
                //update limiting propellant and fuel mass
                FindLimitingPropellant(out limitingPropellant, out limitingResource);
                limtingResource_initialAmount = limitingResource.amount;
                fuelMass = CalculateEffectiveFuelMass();

                //If burnTime or fuelMass change adjust thrustPercent
                if (burnTime != burnTime_last || fuelMass != fuelMass_last)
                {
                    burnTime = RoundToHalf(burnTime);
                    AdjustThrustPercent();
                }

                //If thrustPercent changes adjust burnTime
                else if(thrustPercent != thrustPercent_last)
                {
                    thrustPercent = RoundToHalf(thrustPercent);
                    AdjustBurnTime();
                }

                //If editThrustCurve button is presses, open thrust curve window
                if(editThrustCurve)
                {
                    thrustCurve_guiController.windowPos = GUILayout.Window(this.GetHashCode(), thrustCurve_guiController.windowPos, thrustCurve_guiController.DrawGUI, "Edit Thrust Curve", thrustCurve_guiController.windowStyle);
                }
            }
        }


        /************************************************************************\
         * IonModuleSRB class                                                   *
         * AdjustThrustPercent function                                         *
         *                                                                      *
         * Adjusts the thrustPercent value for changes in burn time or fuelMass.*
        \************************************************************************/
        public void AdjustThrustPercent()
        {
#if DEBUG_UPDATES
            Debug.Log("IonModuleSRB.AdjustThrustPercent() " + this.part.name);
#endif
            //result is multiplied by 2, rounded, multiplied by 0.5 to round to the nearest 0.5
            thrustPercent = CalculateThrust(burnTime) / module_engine.maxThrust * 100f;
            thrustPercent = RoundToHalf(thrustPercent);

            //If thrustPercent is below minium adjust and re-calc burn time
            //(don't use AdjustBurnTime() to avoid posibility of back and forth loop)
            if (thrustPercent < thrustPercent_min)
            {
                thrustPercent = thrustPercent_min;
                burnTime = CalculateBurnTime(thrustPercent / 100f * module_engine.maxThrust);
                burnTime = RoundToHalf(burnTime);
            }
            else if(thrustPercent > 1f)
            {
                thrustPercent = 1f;
                burnTime = CalculateBurnTime(thrustPercent / 100f * module_engine.maxThrust);
                burnTime = RoundToHalf(burnTime);
            }

            UpdateData();

#if DEBUG_UPDATES
            Debug.Log("IonModuleSRB.AdjustThrustPercent(): Recalculated thrust percent to " + thrustPercent);
#endif
        }

        /************************************************************************\
         * IonModuleSRB class                                                   *
         * AdjustBurnTime function                                              *
         *                                                                      *
         * Adjusts the burnTime value for changes in thrustPercent.             *
        \************************************************************************/
        public void AdjustBurnTime()
        {
#if DEBUG_UPDATES
            Debug.Log("IonModuleSRB.AdjustBurnTime() " + this.part.name);
#endif
            //result is multiplied by 2, rounded, multiplied by 0.5 to round to the nearest 0.5
            burnTime = CalculateBurnTime(module_engine.maxThrust * thrustPercent / 100f);
            burnTime = RoundToHalf(burnTime);

            //If burnTime is out of bounds adjust and re-calc thrustPercent
            //Pass the changes off to AjustThrustPercent function
            if (burnTime < burnTime_min)
            {
                burnTime = burnTime_min;
                AdjustThrustPercent();
            }
            else if (burnTime > burnTime_max)
            {
                burnTime = burnTime_max;
                AdjustThrustPercent();
            }
            else
            {
                UpdateData();
            }


#if DEBUG_UPDATES
            Debug.Log("IonModuleSRB.AdjustBurnTime(): Recalculated burn time to " + burnTime);
#endif
        }


        /************************************************************************\
         * IonModuleSRB class                                                   *
         * UpdateData function                                                  *
         *                                                                      *
         * Updates data.  For use after thrustPercent or burnTime has been      *
         * recalculated.                                                        *
        \************************************************************************/
        public void UpdateData()
        {
            //set last variables
            burnTime_last = burnTime;
            fuelMass_last = fuelMass;
            thrustPercent_last = thrustPercent;

            //update engine and thrust display
            module_engine.thrustPercentage = thrustPercent;
            thrust = module_engine.maxThrust * thrustPercent / 100f;

            //update thrust curve
            UpdateThrustCurve();
        }

        /************************************************************************\
         * IonModuleSRB class                                                   *
         * UpdateThrustCurve function                                           *
         *                                                                      *
         * Updates the thrustCurve and thrustCurve_gui with the current         *
         * thrustPortion.                                                       *
        \************************************************************************/
        public void UpdateThrustCurve()
        {
            thrustCurve.ScaleCurve(thrustPercent / 100f);
            thrustCurve_guiController.UpdateCruveTexture();
        }


        /************************************************************************\
         * IonModuleSRB class                                                   *
         * CalculateBurnTime function                                           *
         *                                                                      *
         * Calculates and returns the burn time for a full fuel load at the     *
         * given thrust and atmosphere density.                                 *
         *                                                                      *
         * thrust:      thrust to calculate burn time for.                      * 
         * atmoDensity: atmosphere density to calculate burn time for.          *
         *                                                                      *
         * Returns: Predicted burn time of the SRB with a full fuel load.       *
        \************************************************************************/
        private float CalculateBurnTimeFullFuel(float thrust, float atmoDensity = 1.0f)
        {
#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateBurnTimeFullFuel(" + thrust + ", " + atmoDensity +  ") " + this.part.name);
#endif
            float timeBurn;
            PartResource pResource;
            Dictionary<int, double> propellInitAmounts = new Dictionary<int, double>();

            //Set max fuel load
            foreach (Propellant propellant in module_engine.propellants)
            {
                pResource = (PartResource)this.part.Resources[propellant.name];
                propellInitAmounts.Add(propellant.id, pResource.amount);

                if (propellant.ratio > 0)
                    pResource.amount = pResource.maxAmount;
                else
                    pResource.amount = 0;
            }

            //calculate timeBurn based on full fuel load
            timeBurn = CalculateBurnTime(thrust, atmoDensity);

            //Reset fuel load
            foreach (Propellant propellant in module_engine.propellants)
            {
                double amount;
                if (propellInitAmounts.TryGetValue(propellant.id, out amount))
                {
                    pResource = (PartResource)this.part.Resources[propellant.name];
                    pResource.amount = amount;
                }
            }

#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateBurnTimeFullFuel(): timeBurn = " + timeBurn);
#endif
            return timeBurn;
        }


        /************************************************************************\
         * IonModuleSRB class                                                   *
         * CalculateThrust function                                             *
         *                                                                      *
         * Calculates and returns the thrust need to generate the given burn    *
         * time with the current amount of fuel onboard.                        *
         *                                                                      *
         * burnTime:    desired burn time.                                      * 
         * atmoDensity: atmosphere density to calculate burn time for.          *
         *                                                                      *
         * Returns: thrust required to get the desired burn time at the given   *
         *          atmosphere density.                                         *
        \************************************************************************/
        private float CalculateThrust(float burnTime, float atmoDensity = 1.0f)
        {
#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateThrust(" + burnTime + ", " + atmoDensity + ") " + this.part.name);
#endif
            float thrust = 0;
            float engineMassFlow = 0;
            float mixtureProtion;
            
            mixtureProtion = limitingPropellant.ratio * limitingResource.info.density / CalculateMixtureMass();

            if (0 != burnTime && 0 != mixtureProtion)
            {
                engineMassFlow = (float)limitingResource.amount * limitingResource.info.density / (burnTime * mixtureProtion);
                thrust = engineMassFlow * module_engine.atmosphereCurve.Evaluate(atmoDensity) * 9.8f;
            }

#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateThrust(): limitingPropellant = " + limitingPropellant.name + " | mixtureProtion " + mixtureProtion + " | engineMassFlow " + engineMassFlow + " | thrust = " + thrust);
#endif
            return thrust;
        }


        /************************************************************************\
         * IonModuleSRB class                                                   *
         * FindLimitingPropellant function                                      *
         *                                                                      *
         * Finds and resturns the propellant which will burn up first given     *
         * current quantities onboard.                                          *
         *                                                                      *
         * limitingResource:    output to store PartResource of the limiting    *
         *                      Propellant.                                     *
         * limitingPropellant:  output to store the limiting Propellant.        *
        \************************************************************************/
        private void FindLimitingPropellant(out Propellant limitingPropellant, out PartResource limitingResource)
        {
#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.FindLimitingPropellant() " + this.part.name);
#endif
            limitingResource = null;
            limitingPropellant = null;

            PartResource pResource;

            float limitValue = -1;
            float propellantLimitValue;
            float mixtureMass = CalculateMixtureMass();
            
            foreach (Propellant propellant in module_engine.propellants)
            {
                pResource = (PartResource)this.part.Resources[propellant.name];

                //calculate propellant mixture as follows:
                //mixtureProtion = (propellant.ratio * pResource.info.density) / mixtureMass;
                //propellantLimitValue = (float)pResource.amount * pResource.info.density / mixtureProtion);

                //the lower this value is the more the engine is limited by this resource
                propellantLimitValue = mixtureMass * (float)pResource.amount / propellant.ratio;

                if (limitValue < 0 || propellantLimitValue < limitValue)
                {
                    limitValue = propellantLimitValue;
                    limitingPropellant = propellant;
                    limitingResource = pResource;
                }
            }

#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.FindLimitingPropellant(): limitingPropellant = " + limitingPropellant.name);
#endif
        }

        /************************************************************************\
         * IonModuleSRB class                                                   *
         * CalculateMixtureMass function                                        *
         *                                                                      *
         * Calculates and returns the cumulative mass of all propellants at     *
         * their ratio of use in the engine.                                    *
         *                                                                      *
         * Returns: total mass of all propellant at their ratio.                *
        \************************************************************************/
        private float CalculateMixtureMass()
        {
#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateMixtureMass() " + this.part.name);
#endif
            float mixtureMass = 0;

            foreach (Propellant propellant in module_engine.propellants)
                mixtureMass += propellant.ratio * ((PartResource)this.part.Resources[propellant.name]).info.density;

#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateMixtureMass(): mixtureMass = " + mixtureMass);
#endif
            return mixtureMass;
        }

        /************************************************************************\
         * IonModuleSRB class                                                   *
         * CalculateFuelMass function                                           *
         *                                                                      *
         * Calculates and returns the total mass of all propellants.            *
         *                                                                      *
         * Returns: total mass of all propellants.                              *
        \************************************************************************/
        private float CalculateFuelMass()
        {
#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateFuelMass() " + this.part.name);
#endif
            float fuelMass = 0.0f;
            PartResource pResource;

            foreach(Propellant propellant in module_engine.propellants)
            {
                pResource = this.part.Resources[propellant.name];
                fuelMass += (float)pResource.amount * pResource.info.density;
            }

#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateFuelMass(): fuelMass = " + fuelMass);
#endif
            return fuelMass;
        }

        /************************************************************************\
         * IonModuleSRB class                                                   *
         * CalculateEffectiveFuelMass function                                  *
         *                                                                      *
         * Calculates and returns the mass of burnable fuel (as limited by the  *
         * limiting resource.                                                   *
         *                                                                      *
         * limitingResource and limitingPropelant must be set.                  *
         *                                                                      *
         * Returns: mass of fuel that can be burned before flameout.            *
        \************************************************************************/
        private float CalculateEffectiveFuelMass()
        {
#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateEffectiveFuelMass() " + this.part.name);
#endif
            float fuelMass = 0.0f;

            //limitingResourceMass = limitingResource.amount * limitingResource.info.density 
            //fuelMass = limitingResourceMass * CalculateMixtureMass() / (limitingResource.info.density * limitingPropellant.ratio)
            fuelMass = (float)limitingResource.amount * CalculateMixtureMass() / limitingPropellant.ratio;

#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateEffectiveFuelMass(): limitingPropellant " + limitingPropellant.name + " | fuelMass = " + fuelMass);
#endif
            return fuelMass;
        }

        /************************************************************************\
         * IonModuleSRB class                                                   *
         * CalculateBurnTime function                                           *
         *                                                                      *
         * Calculates and returns the burn time for the current fuel onboard    *
         * at the given thrust and atmosphere density.                          *
         *                                                                      *
         * thrust:      thrust to calculate burn time for.                      * 
         * atmoDensity: atmosphere density to calculate burn time for.          *
         *                                                                      *
         * Returns: Predicted burn time of the SRB.                             *
        \************************************************************************/
        private float CalculateBurnTime(float thrust, float atmoDensity = 1.0f)
        {

#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateBurnTime(" + thrust + ", " + atmoDensity + ") " + this.part.name);
#endif
            float timeBurn = 0;
            float engineMassFlow = 0;

            if (0 != module_engine.atmosphereCurve.Evaluate(atmoDensity))
                engineMassFlow = thrust / (module_engine.atmosphereCurve.Evaluate(atmoDensity) * 9.8f);

            if (0 != engineMassFlow)
            {
                //calculate timeBurn as follows:
                //mixtureProtion = (limitingPropellant.ratio * limitingResource.info.density) / CalculateMixtureMass();
                //timeBurn = (float)limitingResource.amount * limitingResource.info.density / (engineMassFlow * mixtureProtion);
                timeBurn = (float)limitingResource.amount * CalculateMixtureMass() / (engineMassFlow * limitingPropellant.ratio);
            }

#if DEBUG_CALCULATIONS
            Debug.Log("IonModuleSRB.CalculateBurnTime(): limitingPropellant " + limitingPropellant.name + " | engineMassFlow " + engineMassFlow + " | timeBurn = " + timeBurn);
#endif
            return timeBurn;
        }

        /************************************************************************\
         * IonModuleSRB class                                                   *
         * RoundToHalf function                                                 *
         *                                                                      *
         * Rounds value to the nearest 0.5.                                     *
         *                                                                      *
         * value:   value to round.                                             *
         *                                                                      *
         * Returns: value rounded to the nearest 0.5.                           *
        \************************************************************************/
        private float RoundToHalf(float value)
        {
            //result is multiplied by 2, rounded, multiplied by 0.5 to round to the nearest 0.5
            return (float)Math.Round(value * 2f, MidpointRounding.AwayFromZero) * 0.5f;
        }
    }
}
