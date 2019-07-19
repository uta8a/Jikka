module Jikka.Semantics

open System.Numerics

type ValName = ValName of string

type TyName = TyName of string

type IntExpr =
    | LiteralIExp of BigInteger
    | VarIExp of ValName
    | NegateIExp of IntExpr
    | AddIExp of IntExpr * IntExpr
    | SubIExp of IntExpr * IntExpr
    | MulIExp of IntExpr * IntExpr
    | DivIExp of IntExpr * IntExpr
    | ModIExp of IntExpr * IntExpr
    | PowIExp of IntExpr * IntExpr

// base types
type BType =
    | VarBTy of TyName
    | FunBTy of BType * BType
    | ZahlBTy
    | BoolBTy

// refined types
type RType =
    | VarRTy of TyName
    | FunRTy of RType * RType
    | ZahlRTy
    | NatRTy
    | OrdinalRTy of IntExpr
    | RangeRTy of IntExpr * IntExpr
    | BoolRTy

// type schemas
type Schema<'t> =
    | Monotype of 't
    | Polytype of TyName * Schema<'t>

// untyped exprs
type UExpr =
    | VarUExp of int
    | FreeVarUExp of ValName * RType
    | LamUExp of option<RType> * UExpr
    | AppUExp of UExpr * UExpr
    | IfThenElseUExp of UExpr * UExpr * UExpr
    | IntUExp of BigInteger
    | BoolUExp of bool

// typed exprs
type Expr =
    | VarExp of int
    | FreeVarExp of ValName * RType
    | LamExp of RType * Expr
    | AppExp of Expr * Expr
    | IfThenElseExp of Expr * Expr * Expr
    | IntExp of BigInteger
    | BoolExp of bool

let newGensym (callback : string -> 'a) (prefix : string) : unit -> 'a =
    let i = ref 0
    fun () ->
        i := !i + 1
        callback (sprintf "%s%d" prefix (!i))

let rec foldSchema (monotype : 't -> 'a) (polytype : TyName -> 'a -> 'a) : Schema<'t> -> 'a =
    let rec go =
        function
        | Monotype t -> monotype t
        | Polytype(x, scm) -> polytype x (go scm)
    go

let substTyVarOfRType (b : TyName) (a : RType) : RType -> RType =
    let rec go =
        function
        | VarRTy x ->
            if x = b then a
            else VarRTy x
        | FunRTy(t1, t2) -> FunRTy(go t1, go t2)
        | ZahlRTy -> ZahlRTy
        | NatRTy -> NatRTy
        | OrdinalRTy n -> OrdinalRTy n
        | RangeRTy(l, r) -> RangeRTy(l, r)
        | BoolRTy -> BoolRTy
    go

let substTyVarOfConstraints (b : TyName) (a : RType) : list<RType * RType> -> list<RType * RType> = List.map (fun (t1, t2) -> (substTyVarOfRType b a t1, substTyVarOfRType b a t2))
let substTyVarsOfRType (subst : list<TyName * RType>) : RType -> RType = List.foldBack (fun (b, a) t -> substTyVarOfRType b a t) subst

let substTyVarOfExpr (b : TyName) (a : RType) : Expr -> Expr =
    let subst = substTyVarOfRType b a

    let rec go =
        function
        | VarExp x -> VarExp x
        | FreeVarExp(x, t) -> FreeVarExp(x, subst t)
        | LamExp(t, e) -> LamExp(subst t, go e)
        | AppExp(e1, e2) -> AppExp(go e1, go e2)
        | IfThenElseExp(e1, e2, e3) -> IfThenElseExp(go e1, go e2, go e3)
        | IntExp n -> IntExp n
        | BoolExp p -> BoolExp p
    go

let substTyVarsOfExpr (subst : list<TyName * RType>) : Expr -> Expr = List.foldBack (fun (b, a) e -> substTyVarOfExpr b a e) subst
let realizeSchema (gensym : unit -> TyName) : Schema<RType> -> RType = foldSchema id (fun x t -> substTyVarOfRType x (VarRTy(gensym())) t)

let listTypeConstraints (gensym : unit -> TyName) : UExpr -> (Expr * RType * list<RType * RType>) =
    let rec go (stk : list<RType>) (acc : list<RType * RType>) =
        function
        | VarUExp i -> (VarExp i, stk.[i], acc)
        | FreeVarUExp(x, t) -> (FreeVarExp(x, t), t, acc)
        | LamUExp(t, e) ->
            let t =
                match t with
                | None -> VarRTy(gensym())
                | Some t -> t

            let (e, u, acc) = go (t :: stk) acc e
            (LamExp(t, e), FunRTy(t, u), acc)
        | AppUExp(e1, e2) ->
            let (e1, t1, acc) = go stk acc e1
            let (e2, t2, acc) = go stk acc e2
            let s = VarRTy(gensym())
            (AppExp(e1, e2), s, (t1, FunRTy(t2, s)) :: acc)
        | IfThenElseUExp(e1, e2, e3) ->
            let (e1, t1, acc) = go stk acc e1
            let (e2, t2, acc) = go stk acc e2
            let (e3, t3, acc) = go stk acc e3
            (IfThenElseExp(e1, e2, e3), t2, (t1, BoolRTy) :: (t2, t3) :: acc)
        | IntUExp n ->
            let t =
                if n >= 0I then OrdinalRTy(LiteralIExp(n + 1I))
                else ZahlRTy
            (IntExp n, t, acc)
        | BoolUExp p -> (BoolExp p, BoolRTy, acc)
    go [] []

let listFreeTyVarsOfRType (env : list<TyName>) : RType -> list<TyName> =
    let rec go acc t =
        match t with
        | VarRTy x ->
            if List.contains x env then acc
            else x :: acc
        | FunRTy(s, t) -> go (go acc s) t
        | ZahlRTy -> acc
        | NatRTy -> acc
        | OrdinalRTy _ -> acc
        | RangeRTy _ -> acc
        | BoolRTy -> acc
    go []

let listFreeTyVarsOfSchema (env : list<TyName>) (scm : Schema<RType>) : list<TyName> =
    let (evn, t) = foldSchema (fun t -> ([], t)) (fun x (env, t) -> (x :: env, t)) scm
    listFreeTyVarsOfRType env t

let rec isSubtype (t1 : RType) (t2 : RType) : bool =
    match (t1, t2) with
    | _ when t1 = t2 -> true
    | (NatRTy, ZahlRTy) -> true
    | (OrdinalRTy _, ZahlRTy) -> true
    | (OrdinalRTy _, NatRTy) -> true
    | (RangeRTy _, ZahlRTy) -> true
    | (FunRTy(t11, t12), FunRTy(t21, t22)) when isSubtype t21 t11 && isSubtype t12 t22 -> true
    | _ -> false

let rec unifyConstraints : list<RType * RType> -> list<TyName * RType> =
    function
    | [] -> []
    | (t1, t2) :: constraints ->
        match (t1, t2) with
        | _ when t1 = t2 -> unifyConstraints constraints
        | _ when isSubtype t1 t2 -> unifyConstraints constraints
        | _ when isSubtype t2 t1 -> unifyConstraints constraints
        | (VarRTy x, t) when not (List.contains x (listFreeTyVarsOfRType [] t)) ->
            let subst = unifyConstraints (substTyVarOfConstraints x t constraints)
            (x, substTyVarsOfRType subst t) :: subst
        | (t, VarRTy x) when not (List.contains x (listFreeTyVarsOfRType [] t)) ->
            let subst = unifyConstraints (substTyVarOfConstraints x t constraints)
            (x, substTyVarsOfRType subst t) :: subst
        | (FunRTy(t11, t12), FunRTy(t21, t22)) -> unifyConstraints ((t11, t21) :: (t12, t22) :: constraints)
        | (_, _) -> failwithf "failed to unify constraints: %A = %A" t1 t2

// Hindley/Milner type inference
let inferTypes (gensym : unit -> TyName) (e : UExpr) (annot : option<RType>) : Expr * Schema<RType> =
    let (e, t, constraints) = listTypeConstraints gensym e

    let constraints =
        match annot with
        | None -> constraints
        | Some annot -> (t, annot) :: constraints

    let subst = unifyConstraints constraints
    let e = substTyVarsOfExpr subst e
    let t = substTyVarsOfRType subst t
    let scm = Monotype t
    match listFreeTyVarsOfSchema [] scm with
    | [] -> ()
    | _ -> failwithf "the type schema is not closed: %A" scm
    (e, scm)