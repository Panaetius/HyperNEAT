using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Registration;
using System.IO;
using System.Linq;
using System.Reflection;

using HyperNeatLib.ActivationFunctions;
using HyperNeatLib.Interfaces;

namespace HyperNeatLib.Factories
{
    public class ActivationFunctionFactory
    {
        private ActivationFunctionFactory()
        {
            var registration = new RegistrationBuilder();

            registration.ForTypesDerivedFrom<IActivationFunction>().ExportInterfaces();

            var assemblyCatalog = new DirectoryCatalog(
              Path.GetDirectoryName(
               Assembly.GetExecutingAssembly().Location));
            var compositionContainer = new CompositionContainer(assemblyCatalog);
            compositionContainer.ComposeParts(this);
            
            activationFunctions =
                activationFunctions.Where(f => !(f is NullActivationFunction) && !(f is BiasActivationFunction))
                    .ToList();
        }

        public static ActivationFunctionFactory Instance { get; } = new ActivationFunctionFactory();

        [ImportMany(typeof(IActivationFunction))]
        private List<IActivationFunction> activationFunctions;

        private Random random = new Random();

        public IActivationFunction GetRandomActivationFunction()
        {
            return activationFunctions[random.Next(activationFunctions.Count)];
        } 
    }
}