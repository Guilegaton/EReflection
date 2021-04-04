using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Task1.DoNotChange;

namespace Task1
{
    public class Container
    {
        public List<Assembly> Assemblies { get; private set; }
        public Dictionary<Type, Type> TypesDependencies { get; private set; }
        public Dictionary<Type, Func<object>> Types { get; private set; }

        public Container()
        {
            Assemblies = new List<Assembly>();
            TypesDependencies = new Dictionary<Type, Type>();
            Types = new Dictionary<Type, Func<object>>();
        }

        #region  public methods

        /// <summary>
        /// Register all types with attributes in assembly
        /// </summary>
        /// <param name="assembly">Registered assembly</param>
        public void AddAssembly(Assembly assembly)
        {
            if (Assemblies.Any(assem => assem == assembly))
            {
                throw new Exception("This assembly already registered.");
            }
            Assemblies.Add(assembly);

            // Get all types with attributes
            var types = assembly.GetTypes()
                                .Where(type => type.GetCustomAttribute(typeof(ExportAttribute)) != null ||
                                               type.GetProperties().Any(prop => prop.GetCustomAttribute(typeof(ImportAttribute)) != null) ||
                                               type.GetCustomAttribute(typeof(ImportConstructorAttribute)) != null);
            foreach (var type in types)
            {
                AddType(type);
            }
        }

        /// <summary>
        /// Register type. Type can be with ExportAttribute and contract type.
        /// </summary>
        /// <param name="type">Registered type</param>
        public void AddType(Type type)
        {
            var attribute = type.GetCustomAttribute(typeof(ExportAttribute)) as ExportAttribute;
            // Check if type have export attribute with contract type
            if (attribute != null &&
               attribute.Contract != null &&
               (type.GetInterfaces().Any(inter => inter == attribute.Contract) || type.BaseType == attribute.Contract))
            {
                TypesDependencies.Add(attribute.Contract, type);
            }
            // Or register type without contracts
            else
            {
                TypesDependencies.Add(type, type);
            }

            RegisterType(type);
        }

        /// <summary>
        /// Register type using contract type
        /// </summary>
        /// <param name="type">Registered type</param>
        /// <param name="baseType">Registered contract type</param>
        public void AddType(Type type, Type baseType)
        {
            TypesDependencies.Add(baseType, type);
            TypesDependencies.Add(type, type);
            RegisterType(type);
        }

        /// <summary>
        /// Get instance of required type.
        /// </summary>
        /// <typeparam name="T">Registered type</typeparam>
        /// <returns>Instance of object</returns>
        public T Get<T>()
        {
            return (T)Get(typeof(T));
        }

        #endregion

        #region private methods

        private void RegisterType(Type type)
        {
            // Check exception case when type have several constructors
            var constructors = type.GetConstructors();
            if (constructors.Count() > 1)
            {
                throw new Exception($"{type.FullName} have several constructors");
            }

            // Check exception case when type have potential overflowing
            var registredProperties = type.GetProperties()
                .Where(prop => prop.GetCustomAttribute(typeof(ImportAttribute)) != null);
            if (registredProperties.Any(prop => prop.PropertyType == type))
            {
                throw new Exception($"Can't map types with mapped properties of the same type");
            }

            var constructor = constructors.FirstOrDefault();
            var defaultConstructor = constructors.FirstOrDefault(constr => constr.GetParameters().Length == 0);
            // Register type using func mediator.
            if (constructor != defaultConstructor)
            {
                var constructorParameters = constructor.GetParameters();

                Func<object> typeActivator = () => constructor.Invoke(constructorParameters.Select(praram => Get(praram.ParameterType)).ToArray());
                Types.Add(type, typeActivator);
            }
            else if (defaultConstructor != null)
            {
                Func<object> typeActivator = () => defaultConstructor.Invoke(new object[0]);
                Types.Add(type, typeActivator);
            }
        }

        private object Get(Type type)
        {
            var resultType = TypesDependencies[type];
            var resultObject = Types[resultType]();

            var properties = type.GetProperties()
                .Where(prop => prop.GetCustomAttribute(typeof(ImportAttribute)) != null);

            foreach (var prop in properties)
            {
                prop.SetValue(resultObject, Get(prop.PropertyType));
            }

            return resultObject;
        }

        #endregion
    }
}