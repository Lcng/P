using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    // Note: the lack of generic support over method out parameters makes this class very large.
    // Not much is actually going on here, though.
    public class DeclarationTable
    {
        private readonly HashSet<DeclarationTable> children = new HashSet<DeclarationTable>();
        private readonly IDictionary<string, EnumElem> enumElems = new Dictionary<string, EnumElem>();
        private readonly IDictionary<string, PEnum> enums = new Dictionary<string, PEnum>();
        private readonly IDictionary<string, PEvent> events = new Dictionary<string, PEvent>();
        private readonly IDictionary<string, EventSet> eventSets = new Dictionary<string, EventSet>();
        private readonly IDictionary<string, FunctionProto> functionProtos = new Dictionary<string, FunctionProto>();
        private readonly IDictionary<string, Function> functions = new Dictionary<string, Function>();
        private readonly IDictionary<string, Interface> interfaces = new Dictionary<string, Interface>();
        private readonly IDictionary<string, MachineProto> machineProtos = new Dictionary<string, MachineProto>();
        private readonly IDictionary<string, Machine> machines = new Dictionary<string, Machine>();
        private readonly IDictionary<string, StateGroup> stateGroups = new Dictionary<string, StateGroup>();
        private readonly IDictionary<string, State> states = new Dictionary<string, State>();
        private readonly IDictionary<string, TypeDef> typedefs = new Dictionary<string, TypeDef>();
        private readonly IDictionary<string, Variable> variables = new Dictionary<string, Variable>();
        private DeclarationTable parent;

        public DeclarationTable Parent
        {
            get => parent;
            set
            {
                parent?.children.Remove(this);
                parent = value;
                parent?.children.Add(this);
            }
        }

        public IEnumerable<DeclarationTable> Children => children;

        public IEnumerable<IPDecl> AllDecls => enumElems
            .Values.Cast<IPDecl>().Concat(enums.Values).Concat(events.Values).Concat(eventSets.Values).Concat(functionProtos.Values)
            .Concat(functions.Values).Concat(interfaces.Values).Concat(machineProtos.Values).Concat(machines.Values)
            .Concat(stateGroups.Values).Concat(states.Values).Concat(typedefs.Values).Concat(variables.Values);

        public IEnumerable<EnumElem> EnumElems => enumElems.Values;
        public IEnumerable<PEnum> Enums => enums.Values;
        public IEnumerable<PEvent> Events => events.Values;
        public IEnumerable<EventSet> EventSets => eventSets.Values;
        public IEnumerable<FunctionProto> FunctionProtos => functionProtos.Values;
        public IEnumerable<Function> Functions => functions.Values;
        public IEnumerable<Interface> Interfaces => interfaces.Values;
        public IEnumerable<MachineProto> MachineProtos => machineProtos.Values;
        public IEnumerable<Machine> Machines => machines.Values;
        public IEnumerable<StateGroup> StateGroups => stateGroups.Values;
        public IEnumerable<State> States => states.Values;
        public IEnumerable<TypeDef> Typedefs => typedefs.Values;
        public IEnumerable<Variable> Variables => variables.Values;

        #region Overloaded getters
        public bool Get(string name, out EnumElem tree)
        {
            return enumElems.TryGetValue(name, out tree);
        }

        public bool Get(string name, out PEnum tree)
        {
            return enums.TryGetValue(name, out tree);
        }

        public bool Get(string name, out PEvent tree)
        {
            return events.TryGetValue(name, out tree);
        }

        public bool Get(string name, out EventSet tree)
        {
            return eventSets.TryGetValue(name, out tree);
        }

        public bool Get(string name, out FunctionProto tree)
        {
            return functionProtos.TryGetValue(name, out tree);
        }

        public bool Get(string name, out Function tree)
        {
            return functions.TryGetValue(name, out tree);
        }

        public bool Get(string name, out Interface tree)
        {
            return interfaces.TryGetValue(name, out tree);
        }

        public bool Get(string name, out MachineProto tree)
        {
            return machineProtos.TryGetValue(name, out tree);
        }

        public bool Get(string name, out Machine tree)
        {
            return machines.TryGetValue(name, out tree);
        }

        public bool Get(string name, out StateGroup tree)
        {
            return stateGroups.TryGetValue(name, out tree);
        }

        public bool Get(string name, out State tree)
        {
            return states.TryGetValue(name, out tree);
        }

        public bool Get(string name, out TypeDef tree)
        {
            return typedefs.TryGetValue(name, out tree);
        }

        public bool Get(string name, out Variable tree)
        {
            return variables.TryGetValue(name, out tree);
        }
        #endregion

        #region Overloaded lookup methods
        public bool Lookup(string name, out EnumElem tree)
        {
            DeclarationTable current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out PEnum tree)
        {
            DeclarationTable current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out PEvent tree)
        {
            DeclarationTable current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out EventSet tree)
        {
            DeclarationTable current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out FunctionProto tree)
        {
            DeclarationTable current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out Function tree)
        {
            DeclarationTable current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out Interface tree)
        {
            DeclarationTable current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out MachineProto tree)
        {
            DeclarationTable current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out Machine tree)
        {
            DeclarationTable current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out StateGroup tree)
        {
            DeclarationTable current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out State tree)
        {
            DeclarationTable current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out TypeDef tree)
        {
            DeclarationTable current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }

        public bool Lookup(string name, out Variable tree)
        {
            DeclarationTable current = this;
            while (current != null)
            {
                if (current.Get(name, out tree))
                {
                    return true;
                }

                current = current.Parent;
            }

            tree = null;
            return false;
        }
        #endregion

        #region Conflict-checking putters
        public TypeDef Put(string name, PParser.PTypeDefContext tree)
        {
            var typedef = new TypeDef(name, tree);
            CheckConflicts(
                typedef,
                Namespace(typedefs),
                Namespace(enums),
                Namespace(interfaces),
                Namespace(machines),
                Namespace(machineProtos));
            typedefs.Add(name, typedef);
            return typedef;
        }

        public PEnum Put(string name, PParser.EnumTypeDefDeclContext tree)
        {
            var @enum = new PEnum(name, tree);
            CheckConflicts(
                @enum,
                Namespace(enums),
                Namespace(interfaces),
                Namespace(typedefs),
                Namespace(machines),
                Namespace(machineProtos));
            enums.Add(name, @enum);
            return @enum;
        }

        public PEvent Put(string name, PParser.EventDeclContext tree)
        {
            var @event = new PEvent(name, tree);
            CheckConflicts(@event, Namespace(events), Namespace(enumElems));
            events.Add(name, @event);
            return @event;
        }

        public EventSet Put(string name, PParser.EventSetDeclContext tree)
        {
            var eventSet = new EventSet(name, tree);
            CheckConflicts(eventSet, Namespace(eventSets));
            eventSets.Add(name, eventSet);
            return eventSet;
        }

        public Interface Put(string name, PParser.InterfaceDeclContext tree)
        {
            var machineInterface = new Interface(name, tree);
            CheckConflicts(
                machineInterface,
                Namespace(interfaces),
                Namespace(enums),
                Namespace(typedefs),
                Namespace(machines),
                Namespace(machineProtos));
            interfaces.Add(name, machineInterface);
            return machineInterface;
        }

        public Machine Put(string name, PParser.ImplMachineDeclContext tree)
        {
            var machine = new Machine(name, tree);
            CheckConflicts(machine, Namespace(machines), Namespace(interfaces), Namespace(enums), Namespace(typedefs));
            machines.Add(name, machine);
            return machine;
        }

        public MachineProto Put(string name, PParser.ImplMachineProtoDeclContext tree)
        {
            var machineProto = new MachineProto(name, tree);
            CheckConflicts(machineProto, Namespace(machineProtos), Namespace(interfaces), Namespace(enums), Namespace(typedefs));
            machineProtos.Add(name, machineProto);
            return machineProto;
        }

        public Machine Put(string name, PParser.SpecMachineDeclContext tree)
        {
            var specMachine = new Machine(name, tree);
            CheckConflicts(specMachine, Namespace(machines), Namespace(interfaces), Namespace(enums), Namespace(typedefs));
            machines.Add(name, specMachine);
            return specMachine;
        }

        public Function Put(string name, PParser.FunDeclContext tree)
        {
            var function = new Function(name, tree);
            CheckConflicts(function, Namespace(functions));
            functions.Add(name, function);
            return function;
        }

        public FunctionProto Put(string name, PParser.FunProtoDeclContext tree)
        {
            var functionProto = new FunctionProto(name, tree);
            CheckConflicts(functionProto, Namespace(functionProtos));
            functionProtos.Add(name, functionProto);
            return functionProto;
        }

        public StateGroup Put(string name, PParser.GroupContext tree)
        {
            var group = new StateGroup(name, tree);
            CheckConflicts(group, Namespace(stateGroups));
            stateGroups.Add(name, group);
            return group;
        }

        public EnumElem Put(string name, PParser.EnumElemContext tree)
        {
            var enumElem = new EnumElem(name, tree);
            CheckConflicts(enumElem, Namespace(enumElems), Namespace(events));
            enumElems.Add(name, enumElem);
            return enumElem;
        }

        public EnumElem Put(string name, PParser.NumberedEnumElemContext tree)
        {
            var enumElem = new EnumElem(name, tree);
            CheckConflicts(enumElem, Namespace(enumElems), Namespace(events));
            enumElems.Add(name, enumElem);
            return enumElem;
        }

        public Variable Put(string name, PParser.VarDeclContext tree, PParser.IdenContext varName)
        {
            var variable = new Variable(name, tree, varName);
            CheckConflicts(variable, Namespace(variables));
            variables.Add(name, variable);
            return variable;
        }

        public Variable Put(string name, PParser.FunParamContext tree)
        {
            var variable = new Variable(name, tree);
            CheckConflicts(variable, Namespace(variables));
            variables.Add(name, variable);
            return variable;
        }

        public State Put(string name, PParser.StateDeclContext tree)
        {
            var state = new State(name, tree);
            CheckConflicts(state, Namespace(states));
            states.Add(name, state);
            return state;
        }
        #endregion

        #region Conflict API
        // TODO: maybe optimize this?
        private delegate bool TableReader(string name, out IPDecl decl);

        private static void CheckConflicts(IPDecl decl, params TableReader[] namespaces)
        {
            IPDecl existingDecl = null;
            if (namespaces.Any(table => table(decl.Name, out existingDecl)))
            {
                throw new DuplicateDeclarationException(decl, existingDecl);
            }
        }

        private static TableReader Namespace<T>(IDictionary<string, T> table) where T : IPDecl
        {
            return (string name, out IPDecl decl) =>
            {
                bool success = table.TryGetValue(name, out T tDecl);
                decl = tDecl;
                return success;
            };
        }
        #endregion
    }

    public class Variable : IPDecl, ITypedName
    {
        public Variable(string name, PParser.VarDeclContext containingVarDecl, PParser.IdenContext sourceNode)
        {
            Name = name;
            ContainingVarDecl = containingVarDecl;
            SourceNode = sourceNode;
        }

        public Variable(string name, PParser.FunParamContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; set; }
        public PParser.VarDeclContext ContainingVarDecl { get; }
        public bool IsParam => ContainingVarDecl == null;
        public PLanguageType Type { get; set; }
        public ParserRuleContext SourceNode { get; }
    }
    
    public class EnumElem : IPDecl
    {
        public EnumElem(string name, PParser.EnumElemContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public EnumElem(string name, PParser.NumberedEnumElemContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
        public int Value { get; set; }
        public PEnum ParentEnum { get; set; }
    }

    public class StateGroup : IPDecl
    {
        public StateGroup(string name, PParser.GroupContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
        public List<State> States { get; } = new List<State>();
        public List<StateGroup> SubGroups { get; } = new List<StateGroup>();
    }

    public class State : IPDecl
    {
        public State(string name, PParser.StateDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
        public StateTemperature Temperature { get; set; }
        public bool IsStart { get; set; }
        public Function Entry { get; set; }
        public IDictionary<PEvent, IStateAction> Actions { get; } = new Dictionary<PEvent, IStateAction>();
        public Function Exit { get; set; }
    }

    public interface IStateAction
    {
        PEvent Trigger { get; }
    }

    public class EventDefer : IStateAction
    {
        public EventDefer(PEvent trigger)
        {
            Trigger = trigger;
        }

        public PEvent Trigger { get; }
    }

    public class EventIgnore : IStateAction
    {
        public EventIgnore(PEvent trigger)
        {
            Trigger = trigger;
        }

        public PEvent Trigger { get; }
    }

    public class EventGotoState : IStateAction
    {
        public EventGotoState(PEvent trigger, State target, Function transitionFunction)
        {
            Trigger = trigger;
            Target = target;
            TransitionFunction = transitionFunction;
        }

        public PEvent Trigger { get; }
        public State Target { get; }
        public Function TransitionFunction { get; }
    }

    public class EventPushState : IStateAction
    {
        public EventPushState(PEvent trigger, State target)
        {
            Trigger = trigger;
            Target = target;
        }

        public PEvent Trigger { get; }
        public State Target { get; }
    }

    public class EventDoAction : IStateAction
    {
        public PEvent Trigger { get; }
        public Function Target { get; }

        public EventDoAction(PEvent trigger, Function target)
        {
            Trigger = trigger;
            Target = target;
        }
    }

    public interface IConstructibleDecl : IPDecl
    {
        PLanguageType PayloadType { get; }
    }

    public class MachineProto : IConstructibleDecl
    {
        public MachineProto(string name, PParser.ImplMachineProtoDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
        public PLanguageType PayloadType { get; set; } = PrimitiveType.Null;
    }

    public class Machine : IConstructibleDecl
    {
        public Machine(string name, PParser.ImplMachineDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
            IsSpec = false;
        }

        public Machine(string name, PParser.SpecMachineDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
            IsSpec = true;
        }

        public bool IsSpec { get; }
        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
        public PLanguageType PayloadType { get; set; } = PrimitiveType.Null;
        public int Assume { get; set; } = -1;
        public int Assert { get; set; } = -1;
        public List<Interface> Interfaces { get; } = new List<Interface>();
        public EventSet Receives { get; set; }
        public EventSet Sends { get; set; }
        public List<Variable> Fields { get; } = new List<Variable>();
        public List<Function> Methods { get; } = new List<Function>();
        public List<State> States { get; } = new List<State>();
        public State StartState { get; set; }
        public List<StateGroup> Groups { get; } = new List<StateGroup>();
        public EventSet Observes { get; set; }
    }

    public class Interface : IConstructibleDecl
    {
        public Interface(string name, PParser.InterfaceDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
        public PLanguageType PayloadType { get; set; } = PrimitiveType.Null;
        public EventSet ReceivableEvents { get; set; }
    }

    public class TypeDef : IPDecl
    {
        public TypeDef(string name, PParser.TypeDefDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
        public PLanguageType Type { get; set; }
    }

    public class PEnum : IPDecl
    {
        public PEnum(string name, PParser.EnumTypeDefDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
        public IEnumerable<EnumElem> Values => elements;
        public int Count => elements.Count;

        public bool AddElement(EnumElem elem)
        {
            if (elem.ParentEnum == this)
            {
                return false;
            }

            bool? success = elem.ParentEnum?.RemoveElement(elem);
            Debug.Assert(success != false);
            elem.ParentEnum = this;
            elements.Add(elem);
            return true;
        }

        public bool RemoveElement(EnumElem elem)
        {
            if (elem.ParentEnum != this)
            {
                return false;
            }

            bool success = elements.Remove(elem);
            Debug.Assert(success);
            elem.ParentEnum = null;
            return true;
        }

        private readonly HashSet<EnumElem> elements = new HashSet<EnumElem>();
    }

    public interface IPDecl
    {
        string Name { get; }
        ParserRuleContext SourceNode { get; }
    }

    public class EventSet : IPDecl
    {
        public EventSet(string name, PParser.EventSetDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public EventSet(string name, PParser.EventSetLiteralContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
        public SortedSet<PEvent> Events { get; } = new SortedSet<PEvent>(Comparer<PEvent>.Create((ev1, ev2) => string.Compare(ev1.Name, ev2.Name, StringComparison.Ordinal)));
    }

    public class PEvent : IPDecl
    {
        public PEvent(string name, PParser.EventDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
            PayloadType = PrimitiveType.Null;
            Assert = -1;
            Assume = -1;
        }

        public int Assume { get; set; }
        public int Assert { get; set; }
        public PLanguageType PayloadType { get; set; }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
    }

    public class Function : IPDecl
    {
        public Function(string name, PParser.FunDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public Function(PParser.AnonEventHandlerContext sourceNode)
        {
            Name = "";
            SourceNode = sourceNode;
        }

        public Function(PParser.NoParamAnonEventHandlerContext sourceNode)
        {
            Name = "";
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public Machine Owner { get; set; }
        public ParserRuleContext SourceNode { get; }
        public FunctionSignature Signature { get; } = new FunctionSignature();
        public List<Variable> LocalVariables { get; } = new List<Variable>();
    }

    public class FunctionProto : IPDecl
    {
        public FunctionProto(string name, PParser.FunProtoDeclContext sourceNode)
        {
            Name = name;
            SourceNode = sourceNode;
        }

        public string Name { get; }
        public ParserRuleContext SourceNode { get; }
        public FunctionSignature Signature { get; } = new FunctionSignature();
        public List<Machine> Creates { get; set; }
    }

    public class FunctionSignature
    {
        public List<ITypedName> Parameters { get; } = new List<ITypedName>();
        public IEnumerable<PLanguageType> ParameterTypes => Parameters.Select(ty => ty.Type);
        public PLanguageType ReturnType { get; set; } = PrimitiveType.Null;
    }
}