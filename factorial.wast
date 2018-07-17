(module
  (type (;0;) (func (param i32) (result f64)))
  (func (;0;) (type 0) (param i32) (result f64)
    (local i64 i64)
    block  ;; label = @1
      get_local 0
      i32.const 1
      i32.lt_s
      br_if 0 (;@1;)
      get_local 0
      i64.extend_s/i32
      i64.const 1
      i64.add
      set_local 1
      i64.const 1
      set_local 2
      loop  ;; label = @2
        get_local 2
        get_local 1
        i64.const -1
        i64.add
        tee_local 1
        i64.mul
        set_local 2
        get_local 1
        i64.const 1
        i64.gt_s
        br_if 0 (;@2;)
      end
      get_local 2
      f64.convert_s/i64
      return
    end
    f64.const 0x1p+0 (;=1;))
  (table (;0;) 0 anyfunc)
  (memory (;0;) 1)
  (export "memory" (memory 0))
  (export "_Z4facti" (func 0)))
