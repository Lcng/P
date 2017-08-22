using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Antlr4.Runtime.Tree;
using Microsoft.Pc.Antlr;

namespace Microsoft.Pc.TypeChecker
{
    public static class Analyzer
    {
        public static void AnalyzeCompilationUnit(params PParser.ProgramContext[] programUnits)
        {
            var walker = new ParseTreeWalker();
            var topLevelTable = new DeclarationTable();
            var programDeclarations = new ParseTreeProperty<DeclarationTable>();
            var nodesToDeclarations = new ParseTreeProperty<IPDecl>();
            var stubListener = new DeclarationStubListener(topLevelTable, programDeclarations, nodesToDeclarations);
            var declListener = new DeclarationListener(programDeclarations, nodesToDeclarations);

            // Add built-in events to the table.
            topLevelTable.Put("halt", (PParser.EventDeclContext) null);
            topLevelTable.Put("null", (PParser.EventDeclContext) null);

            // Step 1: Create mapping of names to declaration stubs
            foreach (PParser.ProgramContext programUnit in programUnits)
                walker.Walk(stubListener, programUnit);

            // NOW: no declarations have ambiguous names.
            // NOW: there is exactly one declaration object for each declaration.
            // NOW: every declaration object is associated in both directions with its corresponding parse tree node.
            // NOW: enums and their elements are related to one another

            // Step 4: Validate declarations and fill with types
            foreach (PParser.ProgramContext programUnit in programUnits)
                walker.Walk(declListener, programUnit);

            ValidateDeclarations(programDeclarations, nodesToDeclarations, topLevelTable);

            // NOW: all declarations are valid
        }

        [Conditional("DEBUG")]
        private static void ValidateDeclarations(
            ParseTreeProperty<DeclarationTable> programDeclarations,
            ParseTreeProperty<IPDecl> nodesToDeclarations,
            DeclarationTable topLevelTable)
        {
            var validator = new Validator(programDeclarations, nodesToDeclarations);
            foreach (var decl in AllDeclarations(topLevelTable))
            {
                if (!validator.IsValid((dynamic) decl.Item1, decl.Item2))
                    throw new ArgumentException($"malformed declaration {decl.Item1.Name}");
            }
        }

        private static IEnumerable<Tuple<IPDecl, DeclarationTable>> AllDeclarations(DeclarationTable root)
        {
            foreach (IPDecl decl in root.AllDecls)
                yield return Tuple.Create(decl, root);
            foreach (DeclarationTable child in root.Children)
            {
                foreach (var subdecl in AllDeclarations(child))
                    yield return subdecl;
            }
        }
    }

    public class Validator
    {
        private readonly ParseTreeProperty<IPDecl> _nodesToDeclarations;
        private readonly ParseTreeProperty<DeclarationTable> _programDeclarations;

        public Validator(
            ParseTreeProperty<DeclarationTable> programDeclarations,
            ParseTreeProperty<IPDecl> nodesToDeclarations)
        {
            _programDeclarations = programDeclarations;
            _nodesToDeclarations = nodesToDeclarations;
        }

        public bool IsValid(EnumElem enumElem, DeclarationTable sourceTable)
        {
            // every enum element should be found among its parent's elements
            // and the map should point to the correct declaration
            return enumElem.ParentEnum.Values.Contains(enumElem) &&
                   _nodesToDeclarations.Get(enumElem.SourceNode) == enumElem;
        }

        public bool IsValid(EventSet eventSet, DeclarationTable sourceTable)
        {
            return _nodesToDeclarations.Get(eventSet.SourceNode) == eventSet;
        }

        public bool IsValid(Function function, DeclarationTable sourceTable)
        {
            return function.Owner?.Methods.Contains(function) != false && // function properly registered with machine
                   function.Signature.ReturnType != null && // function signature has return type
                   function.Signature.Parameters.All(
                                                     param => param.Type !=
                                                              null) && // function signature parameters have types
                   _nodesToDeclarations.Get(function.SourceNode) == function; // map is bi-directional
        }

        public bool IsValid(FunctionProto functionProto, DeclarationTable sourceTable)
        {
            return functionProto.Signature.ReturnType != null && // function proto has return type
                   functionProto.Signature.Parameters
                                .All(p => p.Type != null) && // function parameters have known types
                   _nodesToDeclarations.Get(functionProto.SourceNode) == functionProto;
        }

        public bool IsValid(Interface pInterface, DeclarationTable sourceTable)
        {
            return pInterface.PayloadType != null && // interface has known payload type
                   _nodesToDeclarations.Get(pInterface.SourceNode) == pInterface;
        }

        private static IEnumerable<State> Flatten(IEnumerable<StateGroup> groups)
        {
            foreach (StateGroup group in groups)
            {
                foreach (State groupState in group.States)
                    yield return groupState;

                foreach (State subState in Flatten(group.SubGroups))
                    yield return subState;
            }
        }

        public bool IsValid(Machine machine, DeclarationTable sourceTable)
        {
            var allStates = machine.States.Concat(Flatten(machine.Groups)).ToList();
            bool success = machine.Methods.All(fun => fun.Owner == machine);
            success &= machine.PayloadType != null;
            success &= machine.StartState != null;
            success &= allStates.Contains(machine.StartState);
            success &= allStates.All(st => !st.IsStart || st.IsStart && st == machine.StartState);
            success &= machine.Fields.All(v => v.IsParam == false);
            success &= _nodesToDeclarations.Get(machine.SourceNode) == machine;
            return success;
        }

        public bool IsValid(MachineProto machineProto, DeclarationTable sourceTable)
        {
            return _nodesToDeclarations.Get(machineProto.SourceNode) == machineProto;
        }

        public bool IsValid(PEnum pEnum, DeclarationTable sourceTable)
        {
            return _nodesToDeclarations.Get(pEnum.SourceNode) == pEnum;
        }

        public bool IsValid(PEvent pEvent, DeclarationTable sourceTable)
        {
            if (pEvent.SourceNode == null)
                return pEvent.Name.Equals("halt") || pEvent.Name.Equals("null");

            return _nodesToDeclarations.Get(pEvent.SourceNode) == pEvent;
        }

        public bool IsValid(State state, DeclarationTable sourceTable)
        {
            return _nodesToDeclarations.Get(state.SourceNode) == state;
        }

        public bool IsValid(StateGroup stateGroup, DeclarationTable sourceTable)
        {
            return _nodesToDeclarations.Get(stateGroup.SourceNode) == stateGroup;
        }

        public bool IsValid(TypeDef typeDef, DeclarationTable sourceTable)
        {
            return _nodesToDeclarations.Get(typeDef.SourceNode) == typeDef;
        }

        public bool IsValid(Variable variable, DeclarationTable sourceTable)
        {
            return _nodesToDeclarations.Get(variable.SourceNode) == variable;
        }
    }
}