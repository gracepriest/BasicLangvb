; ModuleID = 'StdLibDemo'
source_filename = "StdLibDemo.bas"
target datalayout = "e-m:w-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-windows-msvc"

; Format strings for printf
@.fmt.int = private unnamed_addr constant [4 x i8] c"%d\0A\00"
@.fmt.long = private unnamed_addr constant [5 x i8] c"%ld\0A\00"
@.fmt.double = private unnamed_addr constant [4 x i8] c"%f\0A\00"
@.fmt.str = private unnamed_addr constant [4 x i8] c"%s\0A\00"
@.fmt.0 = private unnamed_addr constant [4 x i8] c"%d\0A\00"

; External function declarations
declare i32 @printf(i8*, ...)
declare i32 @puts(i8*)
declare i32 @scanf(i8*, ...)
declare double @sqrt(double)
declare double @pow(double, double)
declare double @sin(double)
declare double @cos(double)
declare double @tan(double)
declare double @log(double)
declare double @exp(double)
declare double @floor(double)
declare double @ceil(double)
declare double @fabs(double)
declare i32 @rand()
declare void @srand(i32)
declare i64 @time(i64*)

define void @Main() {
entry:
  %x.addr = alloca double
  store double 0.0, double* %x.addr
  %result.addr = alloca double
  store double 0.0, double* %result.addr

  store double 16, double* %x.addr
  %t0 = load double, double* %x.addr
  %t1 = call double @sqrt(double %t0)
  store double %t1, double* %result.addr
  %t2 = load double, double* %result.addr
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fmt.0, i64 0, i64 0), double %t2)
  %t3 = call double @fabs(double -5.5)
  store double %t3, double* %result.addr
  %t4 = load double, double* %result.addr
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fmt.0, i64 0, i64 0), double %t4)
  %t5 = call double @pow(i32 2, i32 8)
  store double %t5, double* %result.addr
  %t6 = load double, double* %result.addr
  call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([4 x i8], [4 x i8]* @.fmt.0, i64 0, i64 0), double %t6)
  ret void
}

