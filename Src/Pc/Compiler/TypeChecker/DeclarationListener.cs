using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    public class DeclarationListener : PParserBaseListener
    {
        /// <summary>
        ///     Functions can be nested via anonymous event handlers, so we do need to keep track.
        /// </summary>
        private readonly Stack<Function> functionStack = new Stack<Function>();

        /// <summary>
        ///     Groups can be nested
        /// </summary>
        private readonly Stack<StateGroup> groupStack = new Stack<StateGroup>();

        /// <summary>
        ///     Maps source nodes to the unique declarations they produced.
        /// </summary>
        private readonly ParseTreeProperty<IPDecl> nodesToDeclarations;

        /// <summary>
        ///     Maps source nodes to the scope objects they produced.
        /// </summary>
        private readonly ParseTreeProperty<DeclarationTable> programDeclarations;

        /// <summary>
        ///     Enum declarations can't be nested, so we simply store the most recently encountered
        ///     one in a variable for the listener actions for the elements to access.
        /// </summary>
        private PEnum currentEnum;

        /// <summary>
        ///     Event sets cannot be nested, so we keep track only of the most recent one.
        /// </summary>
        private EventSet currentEventSet;

        /// <summary>
        ///     Function prototypes cannot be nested, so we keep track only of the most recent one.
        /// </summary>
        private FunctionProto currentFunctionProto;

        /// <summary>
        ///     Machines cannot be nested, so we keep track of only the most recent one.
        /// </summary>
        private Machine currentMachine;

        /// <summary>
        ///     There can't be any nested states, so we only keep track of the most recent.
        /// </summary>
        private State currentState;

        /// <summary>
        ///     This keeps track of the current declaration table. The "on every entry/exit" rules handle popping the
        ///     stack using its Parent pointer.
        /// </summary>
        private DeclarationTable table;

        public DeclarationListener(
            ParseTreeProperty<DeclarationTable> programDeclarations,
            ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            this.programDeclarations = programDeclarations;
            this.nodesToDeclarations = nodesToDeclarations;
        }

        /// <summary>
        ///     Gets the current function or null if not in a function context.
        /// </summary>
        private Function CurrentFunction => functionStack.Count > 0 ? functionStack.Peek() : null;

        public override void EnterEventDecl(PParser.EventDeclContext context)
        {
            // EVENT name=Iden
            var pEvent = (PEvent) nodesToDeclarations.Get(context);

            // cardinality?
            bool hasAssume = context.cardinality()?.ASSUME() != null;
            bool hasAssert = context.cardinality()?.ASSERT() != null;
            int cardinality = int.Parse(context.cardinality()?.IntLiteral().GetText() ?? "-1");
            pEvent.Assume = hasAssume ? cardinality : -1;
            pEvent.Assert = hasAssert ? cardinality : -1;

            // (COLON type)?
            pEvent.PayloadType = TypeResolver.ResolveType(context.type(), table);

            // annotationSet?
            if (context.annotationSet() != null)
                throw new NotImplementedException("event annotations");

            // SEMI ;
        }

        public override void EnterEventSetLiteral(PParser.EventSetLiteralContext context)
        {
            // events+=(HALT | Iden) (COMMA events+=(HALT | Iden))* ;
            foreach (IToken contextEvent in context._events)
            {
                string eventName = contextEvent.Text;
                if (!table.Lookup(eventName, out PEvent evt))
                    throw new MissingEventException(currentEventSet, eventName);

                currentEventSet.Events.Add(evt);
            }
        }

        public override void EnterFunDecl(PParser.FunDeclContext context)
        {
            // FUN name=Iden
            var fun = (Function) nodesToDeclarations.Get(context);
            currentMachine?.Methods.Add(fun);
            fun.Owner = currentMachine;

            // LPAREN funParamList? RPAREN
            functionStack.Push(fun); // funParamList builds signature

            // (COLON type)?
            fun.Signature.ReturnType = TypeResolver.ResolveType(context.type(), table);

            // annotationSet?
            if (context.annotationSet() != null)
                throw new NotImplementedException("function annotations");

            // (SEMI |
            if (context.functionBody() == null)
                throw new NotImplementedException("foreign functions");

            // functionBody) ;
            // handled in EnterFunctionBody
        }

        public override void EnterFunParam(PParser.FunParamContext context)
        {
            // name=Iden
            string name = context.name.Text;
            // COLON type ;
            PLanguageType type = TypeResolver.ResolveType(context.type(), table);

            ITypedName param;
            if (currentFunctionProto != null)
            {
                // If we're in a prototype, then we don't look up a variable, we just create a formal parameter
                param = new FormalParameter
                {
                    Name = name,
                    Type = type
                };
            }
            else
            {
                // Otherwise, we're in a function of some sort, and we add the variable to its signature
                bool success = table.Get(name, out Variable variable);
                Debug.Assert(success);
                variable.Type = type;
                param = variable;
            }

            CurrentFunction.Signature.Parameters.Add(param);
        }

        public override void ExitFunDecl(PParser.FunDeclContext context) { functionStack.Pop(); }

        public override void EnterVarDecl(PParser.VarDeclContext context)
        {
            // VAR idenList
            var varNames = context.idenList()._names;
            // COLON type 
            PLanguageType type = TypeResolver.ResolveType(context.type(), table);
            // annotationSet?
            if (context.annotationSet() != null)
                throw new NotImplementedException("variable annotations");
            // SEMI
            foreach (PParser.IdenContext varName in varNames)
            {
                var variable = (Variable) nodesToDeclarations.Get(varName);
                variable.Type = type;

                if (CurrentFunction != null)
                    CurrentFunction.LocalVariables.Add(variable);
                else
                {
                    Debug.Assert(currentMachine != null);
                    currentMachine.Fields.Add(variable);
                }
            }
        }

        public override void EnterGroup(PParser.GroupContext context)
        {
            // GROUP name=Iden
            var group = (StateGroup) nodesToDeclarations.Get(context);
            // LBRACE groupItem* RBRACE
            if (groupStack.Count > 0)
                groupStack.Peek().SubGroups.Add(group);
            else
                currentMachine.Groups.Add(group);

            groupStack.Push(group);
        }

        public override void ExitGroup(PParser.GroupContext context) { groupStack.Pop(); }

        public override void EnterStateDecl(PParser.StateDeclContext context)
        {
            currentState = (State) nodesToDeclarations.Get(context);
            if (groupStack.Count > 0)
                groupStack.Peek().States.Add(currentState);
            else
                currentMachine.States.Add(currentState);

            // START?
            currentState.IsStart = context.START() != null;
            if (currentState.IsStart)
            {
                if (currentMachine.StartState != null)
                    throw new DuplicateStartStateException(currentMachine, currentState);
                currentMachine.StartState = currentState;
            }

            // temperature=(HOT | COLD)?
            currentState.Temperature = context.temperature == null
                                           ? StateTemperature.WARM
                                           : context.temperature.Text.Equals("HOT")
                                               ? StateTemperature.HOT
                                               : StateTemperature.COLD;

            // STATE name=Iden
            // handled above with lookup.

            // annotationSet?
            if (context.annotationSet() != null)
                throw new NotImplementedException("state annotations");

            // LBRACE stateBodyItem* RBRACE
            // handled by StateEntry / StateExit / StateDefer / StateIgnore / OnEventDoAction / OnEventPushState / OnEventGotoState
        }

        public override void ExitStateDecl(PParser.StateDeclContext context)
        {
            if (currentState.IsStart)
            {
                // The machine's payload type is the start state's entry payload type (or null, by default)
                currentMachine.PayloadType = currentState.Entry?.Signature.ReturnType ?? PrimitiveType.Null;
            }
            currentState = null;
        }

        public override void EnterStateEntry(PParser.StateEntryContext context)
        {
            // (
            Function fun;
            if (context.anonEventHandler() != null)
            {
                // ENTRY anonEventHandler 
                fun = new Function(context.anonEventHandler()) {Owner = currentMachine};
            }
            else // |
            {
                // ENTRY funName=Iden)
                if (!table.Lookup(context.funName.Text, out fun))
                {
                    // TODO: allow prototype state entries
                    if (table.Lookup(context.funName.Text, out FunctionProto proto))
                        throw new NotImplementedException("function prototypes for state entries");
                    throw new MissingDeclarationException(context.funName.Text, context);
                }
            }
            // SEMI
            if (currentState.Entry != null)
                throw new DuplicateEntryException(currentState);
            currentState.Entry = fun;
            functionStack.Push(fun);
        }

        public override void ExitStateEntry(PParser.StateEntryContext context) { functionStack.Pop(); }

        public override void EnterOnEventDoAction(PParser.OnEventDoActionContext context)
        {
            // annotationSet?
            if (context.annotationSet() != null)
                throw new NotImplementedException("state action annotations");

            Function fun;
            if (context.anonEventHandler() != null)
            {
                // DO [...] anonEventHandler
                fun = new Function(context.anonEventHandler()) {Owner = currentMachine};
            }
            else
            {
                // DO funName=Iden
                if (!table.Lookup(context.funName.Text, out fun))
                {
                    if (table.Lookup(context.funName.Text, out FunctionProto proto))
                        throw new NotImplementedException("function prototypes for state actions");
                    throw new MissingDeclarationException(context.funName.Text, context);
                }
            }

            // ON eventList
            foreach (PParser.EventIdContext eventIdContext in context.eventList().eventId())
            {
                if (!table.Lookup(eventIdContext.GetText(), out PEvent evt))
                    throw new MissingDeclarationException(eventIdContext.GetText(), eventIdContext);
                if (currentState.Actions.ContainsKey(evt))
                    throw new DuplicateHandlerException(evt, currentState);
                currentState.Actions.Add(evt, new EventDoAction(evt, fun));
            }

            // SEMI
            functionStack.Push(fun);
        }

        public override void ExitOnEventDoAction(PParser.OnEventDoActionContext context) { functionStack.Pop(); }

        public override void EnterStateExit(PParser.StateExitContext context)
        {
            // EXIT
            Function fun;
            if (context.noParamAnonEventHandler() != null)
            {
                // noParamAnonEventHandler
                fun = new Function(context.noParamAnonEventHandler()) {Owner = currentMachine};
            }
            else
            {
                // funName=Iden
                if (!table.Lookup(context.funName.Text, out fun))
                {
                    if (table.Lookup(context.funName.Text, out FunctionProto proto))
                        throw new NotImplementedException("function prototypes for state exits");
                    throw new MissingDeclarationException(context.funName.Text, context);
                }
            }
            // SEMI
            if (currentState.Exit != null)
                throw new DuplicateExitException(currentState);
            currentState.Exit = fun;
            functionStack.Push(fun);
        }

        public override void ExitStateExit(PParser.StateExitContext context) { functionStack.Pop(); }

        public override void EnterOnEventGotoState(PParser.OnEventGotoStateContext context)
        {
            // annotationSet?
            if (context.annotationSet() != null)
                throw new NotImplementedException("state transition annotations");

            Function transitionFunction;
            if (context.funName != null)
            {
                // WITH funName=Iden
                if (!table.Lookup(context.funName.Text, out transitionFunction))
                    throw new MissingDeclarationException(context.funName.Text, context);
            }
            else if (context.anonEventHandler() != null)
            {
                // WITH anonEventHandler
                transitionFunction = new Function(context.anonEventHandler()) {Owner = currentMachine};
            }
            else
            {
                // SEMI
                transitionFunction = null;
            }
            functionStack.Push(transitionFunction);

            // GOTO stateName 
            State target = FindState(context.stateName());

            // ON eventList
            foreach (PParser.EventIdContext eventIdContext in context.eventList().eventId())
            {
                if (!table.Lookup(eventIdContext.GetText(), out PEvent evt))
                    throw new MissingDeclarationException(eventIdContext.GetText(), eventIdContext);

                if (currentState.Actions.ContainsKey(evt))
                    throw new DuplicateHandlerException(evt, currentState);

                currentState.Actions.Add(evt, new EventGotoState(evt, target, transitionFunction));
            }
        }

        /// <summary>
        ///     Navigate declaration tables in current context to find event in groups named by stateName
        /// </summary>
        /// <param name="stateName">The parse tree naming a state</param>
        /// <returns>The state referenced by the name context</returns>
        private State FindState(PParser.StateNameContext stateName)
        {
            // Starting from machine table...
            DeclarationTable currTable = programDeclarations.Get(currentMachine.SourceNode);
            if (stateName._groups.Count > 0)
            {
                foreach (IToken groupName in stateName._groups)
                {
                    if (!currTable.Get(groupName.Text, out StateGroup group))
                        throw new MissingDeclarationException(groupName.Text, stateName);
                    currTable = programDeclarations.Get(group.SourceNode);
                }
            }
            // ...and get the state or throw
            Debug.Assert(currTable != null);
            if (!currTable.Get(stateName.state.Text, out State target))
                throw new MissingDeclarationException(stateName.state.Text, stateName);
            return target;
        }

        public override void ExitOnEventGotoState(PParser.OnEventGotoStateContext context) { functionStack.Pop(); }

        public override void EnterStateIgnore(PParser.StateIgnoreContext context)
        {
            // annotationSet? 
            if (context.annotationSet() != null)
                throw new NotImplementedException("event ignore annotations");
            // IGNORE nonDefaultEventList
            foreach (IToken token in context.nonDefaultEventList()._events)
            {
                if (!table.Lookup(token.Text, out PEvent evt))
                    throw new MissingDeclarationException(token.Text, context.nonDefaultEventList());
                if (currentState.Actions.ContainsKey(evt))
                    throw new DuplicateHandlerException(evt, currentState);
                currentState.Actions.Add(evt, new EventIgnore(evt));
            }
        }

        public override void EnterStateDefer(PParser.StateDeferContext context)
        {
            // annotationSet? SEMI
            if (context.annotationSet() != null)
                throw new NotImplementedException("event defer annotations");
            // DEFER nonDefaultEventList 
            foreach (IToken token in context.nonDefaultEventList()._events)
            {
                if (!table.Lookup(token.Text, out PEvent evt))
                    throw new MissingDeclarationException(token.Text, context.nonDefaultEventList());
                if (currentState.Actions.ContainsKey(evt))
                    throw new DuplicateHandlerException(evt, currentState);
                currentState.Actions.Add(evt, new EventDefer(evt));
            }
        }

        public override void EnterOnEventPushState(PParser.OnEventPushStateContext context)
        {
            //annotationSet? 
            if (context.annotationSet() != null)
                throw new NotImplementedException("push state annotations");

            // PUSH stateName 
            State targetState = FindState(context.stateName());
            // ON eventList
            foreach (PParser.EventIdContext token in context.eventList().eventId())
            {
                if (!table.Lookup(token.GetText(), out PEvent evt))
                    throw new MissingDeclarationException(token.GetText(), context.eventList());
                if (currentState.Actions.ContainsKey(evt))
                    throw new DuplicateHandlerException(evt, currentState);
                currentState.Actions.Add(evt, new EventPushState(evt, targetState));
            }
        }

        public override void EnterImplMachineProtoDecl(PParser.ImplMachineProtoDeclContext context)
        {
            var proto = (MachineProto) nodesToDeclarations.Get(context);
            proto.PayloadType = TypeResolver.ResolveType(context.type(), table);
        }

        public override void EnterSpecMachineDecl(PParser.SpecMachineDeclContext context)
        {
            // SPEC name=Iden 
            var specMachine = (Machine) nodesToDeclarations.Get(context);
            // OBSERVES eventSetLiteral
            specMachine.Observes = new EventSet($"{specMachine.Name}$eventset", context.eventSetLiteral());
            currentEventSet = specMachine.Observes;
            // machineBody
            currentMachine = specMachine;
        }

        public override void ExitSpecMachineDecl(PParser.SpecMachineDeclContext context)
        {
            currentEventSet = null;
            currentMachine = null;
            var specMachine = (Machine)nodesToDeclarations.Get(context);
            if (specMachine.StartState == null)
            {
                throw new NotImplementedException("machines with no start state");
            }
        }

        public override void EnterFunProtoDecl(PParser.FunProtoDeclContext context)
        {
            // EXTERN FUN name=Iden annotationSet?
            var proto = (FunctionProto) nodesToDeclarations.Get(context);

            // (CREATES idenList? SEMI)?
            if (context.idenList() != null)
            {
                foreach (PParser.IdenContext machineNameToken in context.idenList()._names)
                {
                    if (!table.Lookup(machineNameToken.GetText(), out Machine machine))
                        throw new MissingDeclarationException(machineNameToken.GetText(), context.idenList());
                    proto.Creates.Add(machine);
                }
            }

            // (COLON type)?
            proto.Signature.ReturnType = TypeResolver.ResolveType(context.type(), table);

            // LPAREN funParamList? RPAREN 
            currentFunctionProto = proto;
        }

        public override void ExitFunProtoDecl(PParser.FunProtoDeclContext context) { currentFunctionProto = null; }

        public override void EnterEveryRule(ParserRuleContext ctx)
        {
            DeclarationTable thisTable = programDeclarations.Get(ctx);
            if (thisTable != null)
                table = thisTable;
        }

        public override void ExitEveryRule(ParserRuleContext context)
        {
            if (programDeclarations.Get(context) != null)
            {
                Debug.Assert(table != null);
                // pop the stack
                table = table.Parent;
            }
        }

        public override void EnterEventSetDecl(PParser.EventSetDeclContext context)
        {
            currentEventSet = (EventSet) nodesToDeclarations.Get(context);
        }

        public override void ExitEventSetDecl(PParser.EventSetDeclContext context) { currentEventSet = null; }

        public override void EnterInterfaceDecl(PParser.InterfaceDeclContext context)
        {
            // TYPE name=Iden
            var mInterface = (Interface) nodesToDeclarations.Get(context);

            // LPAREN type? RPAREN
            mInterface.PayloadType = TypeResolver.ResolveType(context.type(), table);

            if (context.eventSet == null)
            {
                // ASSIGN LBRACE eventSetLiteral RBRACE
                // ... or let the eventSetLiteral handler fill in a newly created event set
                Debug.Assert(context.eventSetLiteral() != null);
                mInterface.ReceivableEvents = new EventSet($"{mInterface.Name}$eventset", context.eventSetLiteral());
            }
            else
            {
                // ASSIGN eventSet=Iden
                // Either look up the event set and establish the link by name...
                if (!table.Lookup(context.eventSet.Text, out EventSet eventSet))
                    throw new MissingDeclarationException(context.eventSet.Text, context);

                mInterface.ReceivableEvents = eventSet;
            }

            currentEventSet = mInterface.ReceivableEvents;
        }

        public override void ExitInterfaceDecl(PParser.InterfaceDeclContext context) { currentEventSet = null; }

        public override void EnterPTypeDef(PParser.PTypeDefContext context)
        {
            // TYPE name=Iden 
            var typedef = (TypeDef) nodesToDeclarations.Get(context);

            // ASSIGN type
            typedef.Type = TypeResolver.ResolveType(context.type(), table);
        }

        public override void EnterForeignTypeDef(PParser.ForeignTypeDefContext context)
        {
            // TYPE name=Iden SEMI
            throw new NotImplementedException("foreign types");
        }

        public override void EnterEnumTypeDefDecl(PParser.EnumTypeDefDeclContext context)
        {
            // ENUM name=Iden LBRACE enumElemList RBRACE | ENUM name = Iden LBRACE numberedEnumElemList RBRACE
            currentEnum = (PEnum) nodesToDeclarations.Get(context);
        }

        public override void EnterEnumElem(PParser.EnumElemContext context)
        {
            // name=Iden
            var elem = (EnumElem) nodesToDeclarations.Get(context);
            elem.Value = currentEnum.Count; // listener visits from left-to-right, so this will count upwards correctly.
            bool success = currentEnum.AddElement(elem);
            Debug.Assert(success);
        }

        public override void EnterNumberedEnumElem(PParser.NumberedEnumElemContext context)
        {
            // name=Iden 
            var elem = (EnumElem) nodesToDeclarations.Get(context);
            // ASSIGN value=IntLiteral
            elem.Value = int.Parse(context.value.Text);
            bool success = currentEnum.AddElement(elem);
            Debug.Assert(success);
        }

        public override void EnterImplMachineDecl(PParser.ImplMachineDeclContext context)
        {
            // eventDecl : MACHINE name=Iden
            currentMachine = (Machine) nodesToDeclarations.Get(context);

            // cardinality?
            bool hasAssume = context.cardinality()?.ASSUME() != null;
            bool hasAssert = context.cardinality()?.ASSERT() != null;
            int cardinality = int.Parse(context.cardinality()?.IntLiteral().GetText() ?? "-1");
            currentMachine.Assume = hasAssume ? cardinality : -1;
            currentMachine.Assert = hasAssert ? cardinality : -1;

            // annotationSet?
            if (context.annotationSet() != null)
                throw new NotImplementedException("machine annotations");

            // (COLON idenList)?
            if (context.idenList() != null)
            {
                var interfaces = context.idenList()._names.Select(name => name.GetText());
                foreach (string pInterfaceName in interfaces)
                {
                    if (!table.Lookup(pInterfaceName, out Interface pInterface))
                        throw new MissingDeclarationException(pInterfaceName, context.idenList());

                    currentMachine.Interfaces.Add(pInterface);
                }
            }

            // receivesSends*
            // handled by EnterReceivesSends

            // machineBody
            // handled by EnterVarDecl / EnterFunDecl / EnterGroup / EnterStateDecl
        }

        public override void ExitImplMachineDecl(PParser.ImplMachineDeclContext context)
        {
            currentMachine = null;
            var machine = (Machine)nodesToDeclarations.Get(context);
            if (machine.StartState == null)
            {
                throw new NotImplementedException("machines with no start state");
            }
        }

        public override void EnterMachineReceive(PParser.MachineReceiveContext context)
        {
            // RECEIVES eventSetLiteral? SEMI
            if (currentMachine.Receives == null)
                currentMachine.Receives = new EventSet($"{currentMachine.Name}$receives", context.eventSetLiteral());
            currentEventSet = currentMachine.Receives;
        }

        public override void ExitMachineReceive(PParser.MachineReceiveContext context) { currentEventSet = null; }

        public override void EnterMachineSend(PParser.MachineSendContext context)
        {
            // SENDS eventSetLiteral? SEMI
            if (currentMachine.Sends == null)
                currentMachine.Sends = new EventSet($"{currentMachine.Name}$sends", context.eventSetLiteral());
            currentEventSet = currentMachine.Sends;
        }

        public override void ExitMachineSend(PParser.MachineSendContext context) { currentEventSet = null; }
    }

    public class DuplicateExitException : Exception
    {
        public DuplicateExitException(State state) { State = state; }

        public State State { get; }
    }

    public class DuplicateHandlerException : Exception
    {
        public DuplicateHandlerException(PEvent badEvent, State inState)
        {
            BadEvent = badEvent;
            InState = inState;
        }

        public PEvent BadEvent { get; }
        public State InState { get; }
    }

    public class DuplicateEntryException : Exception
    {
        public DuplicateEntryException(State state) { State = state; }

        public State State { get; }
    }

    public class DuplicateStartStateException : Exception
    {
        public DuplicateStartStateException(Machine currentMachine, State conflictingStartState)
        {
            CurrentMachine = currentMachine;
            ConflictingStartState = conflictingStartState;
        }

        public Machine CurrentMachine { get; }
        public State ConflictingStartState { get; }
    }
}